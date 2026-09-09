using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class ParameterClrTypeFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WidenType(migrationBuilder, "job_parameter");
            WidenType(migrationBuilder, "job_run_parameter");
            WidenType(migrationBuilder, "job_run_result");
            WidenType(migrationBuilder, "job_schedule_parameter");
            WidenType(migrationBuilder, "job_trigger_parameter");

            migrationBuilder.Sql(BuildConversionSql());

            migrationBuilder.DropColumn(name: "allow_multiple", schema: "job", table: "job_parameter");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_multiple",
                schema: "job",
                table: "job_parameter",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            ShrinkType(migrationBuilder, "job_parameter");
            ShrinkType(migrationBuilder, "job_run_parameter");
            ShrinkType(migrationBuilder, "job_run_result");
            ShrinkType(migrationBuilder, "job_schedule_parameter");
            ShrinkType(migrationBuilder, "job_trigger_parameter");
        }

        private static void WidenType(MigrationBuilder migrationBuilder, string table)
            => migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "job",
                table: table,
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15);

        private static void ShrinkType(MigrationBuilder migrationBuilder, string table)
            => migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "job",
                table: table,
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

        private static string BuildConversionSql()
        {
            var remap = TypeRemapSql();
            return $$"""
                CREATE OR REPLACE FUNCTION job.lyo_clr_encode_value(p_type text, p_value text, p_allow_multiple boolean)
                RETURNS text
                LANGUAGE plpgsql
                AS $fn$
                DECLARE
                  encoded text;
                  as_array boolean := false;
                BEGIN
                  IF p_value IS NULL OR btrim(p_value) = '' THEN
                    RETURN p_value;
                  END IF;

                  IF coalesce(p_allow_multiple, false) THEN
                    BEGIN
                      as_array := jsonb_typeof(p_value::jsonb) = 'array';
                    EXCEPTION WHEN others THEN
                      as_array := false;
                    END;
                    IF as_array THEN
                      RETURN p_value;
                    END IF;
                  END IF;

                  IF p_type = 'Json' THEN
                    BEGIN
                      PERFORM p_value::json;
                      encoded := p_value;
                    EXCEPTION WHEN others THEN
                      encoded := to_json(p_value)::text;
                    END;
                  ELSIF p_type IN ('Int', 'Long', 'Decimal') THEN
                    IF p_value ~ '^-?[0-9]+(\.[0-9]+)?([eE][+-]?[0-9]+)?$' THEN
                      encoded := to_json(p_value::numeric)::text;
                    ELSE
                      encoded := to_json(p_value)::text;
                    END IF;
                  ELSIF p_type = 'Bool' THEN
                    encoded := to_json(lower(p_value) IN ('true', '1', 'yes', 'y', 'on'))::text;
                  ELSE
                    encoded := to_json(p_value)::text;
                  END IF;

                  IF coalesce(p_allow_multiple, false) THEN
                    BEGIN
                      IF jsonb_typeof(encoded::jsonb) <> 'array' THEN
                        encoded := jsonb_build_array(encoded::jsonb)::text;
                      END IF;
                    EXCEPTION WHEN others THEN
                      encoded := jsonb_build_array(to_json(p_value)::jsonb)::text;
                    END;
                  END IF;

                  RETURN encoded;
                END;
                $fn$;

                UPDATE job.job_parameter
                SET value = job.lyo_clr_encode_value(type, value, allow_multiple)
                WHERE value IS NOT NULL AND btrim(value) <> '';

                UPDATE job.job_run_parameter
                SET value = job.lyo_clr_encode_value(type, value, false)
                WHERE value IS NOT NULL AND btrim(value) <> '';

                UPDATE job.job_run_result
                SET value = job.lyo_clr_encode_value(type, value, false)
                WHERE value IS NOT NULL AND btrim(value) <> '';

                UPDATE job.job_schedule_parameter
                SET value = job.lyo_clr_encode_value(type, value, false)
                WHERE value IS NOT NULL AND btrim(value) <> '';

                UPDATE job.job_trigger_parameter
                SET value = job.lyo_clr_encode_value(type, value, false)
                WHERE value IS NOT NULL AND btrim(value) <> '';

                {{CollapseSql("job.job_run_parameter", "job_run_id", true)}}
                {{CollapseSql("job.job_schedule_parameter", "job_schedule_id", false)}}
                {{CollapseSql("job.job_trigger_parameter", "job_trigger_id", false)}}

                UPDATE job.job_parameter
                SET type = {{remap}}
                FROM (SELECT id, allow_multiple AS is_list FROM job.job_parameter) s
                WHERE job.job_parameter.id = s.id;

                UPDATE job.job_run_parameter SET type = {{remap.Replace("s.is_list", IsJsonArraySql("value"))}};
                UPDATE job.job_run_result SET type = {{remap.Replace("s.is_list", "false")}};
                UPDATE job.job_schedule_parameter SET type = {{remap.Replace("s.is_list", IsJsonArraySql("value"))}};
                UPDATE job.job_trigger_parameter SET type = {{remap.Replace("s.is_list", IsJsonArraySql("value"))}};

                DROP FUNCTION job.lyo_clr_encode_value(text, text, boolean);
                """;
        }

        private static string CollapseSql(string qualifiedTable, string parentColumn, bool hasEncrypted)
        {
            var encPredicate = hasEncrypted
                ? "bool_or(encrypted_value IS NOT NULL AND (value IS NULL OR btrim(value) = ''))"
                : "false";
            var groups = $"""
                SELECT {parentColumn} AS parent_id, key,
                       {encPredicate} AS skip_collapse,
                       (array_agg(id ORDER BY id))[1] AS keep_id
                FROM {qualifiedTable}
                GROUP BY {parentColumn}, key
                HAVING count(*) > 1
                """;
            return $"""
                WITH groups AS (
                  {groups}
                ),
                agg AS (
                  SELECT g.keep_id, jsonb_agg(t.value::jsonb ORDER BY t.id) AS arr
                  FROM groups g
                  JOIN {qualifiedTable} t ON t.{parentColumn} = g.parent_id AND t.key = g.key
                  WHERE NOT g.skip_collapse
                  GROUP BY g.keep_id
                )
                UPDATE {qualifiedTable} t
                SET value = agg.arr::text
                FROM agg
                WHERE t.id = agg.keep_id;

                WITH groups AS (
                  {groups}
                )
                DELETE FROM {qualifiedTable} t
                USING groups g
                WHERE t.{parentColumn} = g.parent_id AND t.key = g.key AND t.id <> g.keep_id AND NOT g.skip_collapse;
                """;
        }

        private static string IsJsonArraySql(string column)
            => $"(CASE WHEN {column} IS NULL OR btrim({column}) = '' THEN false WHEN left(btrim({column}), 1) = '[' THEN true ELSE false END)";

        private static string TypeRemapSql()
        {
            string n(Type t) => t.FullName!.Replace("'", "''");
            return $"""
                CASE
                  WHEN type = 'String' AND s.is_list THEN '{n(typeof(List<string>))}'
                  WHEN type = 'String' THEN '{n(typeof(string))}'
                  WHEN type = 'Int' AND s.is_list THEN '{n(typeof(List<int>))}'
                  WHEN type = 'Int' THEN '{n(typeof(int))}'
                  WHEN type = 'Long' AND s.is_list THEN '{n(typeof(List<long>))}'
                  WHEN type = 'Long' THEN '{n(typeof(long))}'
                  WHEN type = 'Guid' AND s.is_list THEN '{n(typeof(List<Guid>))}'
                  WHEN type = 'Guid' THEN '{n(typeof(Guid))}'
                  WHEN type = 'Bool' AND s.is_list THEN '{n(typeof(List<bool>))}'
                  WHEN type = 'Bool' THEN '{n(typeof(bool))}'
                  WHEN type = 'Decimal' AND s.is_list THEN '{n(typeof(List<decimal>))}'
                  WHEN type = 'Decimal' THEN '{n(typeof(decimal))}'
                  WHEN type = 'DateTime' AND s.is_list THEN '{n(typeof(List<DateTime>))}'
                  WHEN type = 'DateTime' THEN '{n(typeof(DateTime))}'
                  WHEN type = 'DateOnly' AND s.is_list THEN '{n(typeof(List<DateOnly>))}'
                  WHEN type = 'DateOnly' THEN '{n(typeof(DateOnly))}'
                  WHEN type = 'TimeOnly' AND s.is_list THEN '{n(typeof(List<TimeOnly>))}'
                  WHEN type = 'TimeOnly' THEN '{n(typeof(TimeOnly))}'
                  WHEN type = 'Enum' AND s.is_list THEN '{n(typeof(List<string>))}'
                  WHEN type = 'Enum' THEN '{n(typeof(Enum))}'
                  WHEN type = 'Json' AND s.is_list THEN '{n(typeof(JsonArray))}'
                  WHEN type = 'Json' THEN '{n(typeof(JsonNode))}'
                  WHEN type = 'Regex' AND s.is_list THEN '{n(typeof(List<string>))}'
                  WHEN type = 'Regex' THEN '{n(typeof(Regex))}'
                  WHEN type = 'Xml' AND s.is_list THEN '{n(typeof(List<string>))}'
                  WHEN type = 'Xml' THEN '{n(typeof(XDocument))}'
                  WHEN type = 'Unknown' AND s.is_list THEN '{n(typeof(JsonArray))}'
                  WHEN type = 'Unknown' THEN '{n(typeof(object))}'
                  ELSE type
                END
                """;
        }
    }
}
