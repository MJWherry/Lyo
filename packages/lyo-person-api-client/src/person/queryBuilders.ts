import type {ProjectionQueryReq, QueryConcreteReq, QueryRequestOptions,} from "../models/queryRequests.js";
import {DEFAULT_SOURCE_FILTER_VALUES} from "./sourceEntityTypes.js";

export const DEFAULT_PERSON_INCLUDES = [
    "contactphonenumbers.phonenumber",
    "contactemailaddresses.emailaddress",
    "contactaddresses.address",
];

export const DEFAULT_PERSON_SELECT_FIELDS = [
    "Id",
    "FirstName",
    "LastName",
    "SourceEntityType",
    "contactaddresses.address.city",
];

export const QUERY_FIELD_SOURCE = "SourceEntityType";

export function buildOptions(
    options: Partial<QueryRequestOptions> = {}
): QueryRequestOptions {
    return {
        TotalCountMode: options.TotalCountMode ?? "None",
        IncludeFilterMode: options.IncludeFilterMode ?? "Full",
        ZipSiblingCollectionSelections: options.ZipSiblingCollectionSelections,
    };
}

export function baselineQuery({
                                  start = 0,
                                  amount = 1000,
                              }: {
    start?: number;
    amount?: number;
} = {}): QueryConcreteReq {
    return {
        Options: buildOptions(),
        Start: start,
        Amount: amount,
        Include: [],
        SortBy: [],
    };
}

export function filterSortQuery({
                                    start = 0,
                                    amount = 1000,
                                    sourceFilterValues = DEFAULT_SOURCE_FILTER_VALUES,
                                }: {
    start?: number;
    amount?: number;
    sourceFilterValues?: string;
} = {}): QueryConcreteReq {
    return {
        Options: buildOptions(),
        Start: start,
        Amount: amount,
        whereClause: {
            $type: "group",
            Operator: "Or",
            Children: [
                {
                    $type: "group",
                    Operator: "And",
                    Children: [
                        {$type: "condition", Field: "FirstName", Comparison: "NotEquals", Value: null},
                        {$type: "condition", Field: "LastName", Comparison: "NotEquals", Value: null},
                    ],
                },
                {
                    $type: "condition",
                    Field: QUERY_FIELD_SOURCE,
                    Comparison: "In",
                    Value: sourceFilterValues,
                },
            ],
        },
        SortBy: [
            {PropertyName: "LastName", Direction: "Asc", Priority: 0},
            {PropertyName: "FirstName", Direction: "Asc", Priority: 1},
            {PropertyName: "Id", Direction: "Desc", Priority: 2},
        ],
    };
}

export function complexWhereClause({
                                       include = [],
                                       start = 0,
                                       amount = 1200,
                                   }: {
    include?: string[];
    start?: number;
    amount?: number;
} = {}): QueryConcreteReq {
    return {
        Options: buildOptions(),
        Start: start,
        Amount: amount,
        Include: include,
        SortBy: [
            {PropertyName: "LastName", Direction: "Asc", Priority: 0},
            {PropertyName: "FirstName", Direction: "Asc", Priority: 1},
        ],
        whereClause: {
            $type: "group",
            Operator: "And",
            Children: [
                {
                    $type: "condition",
                    Field: "FirstName",
                    Comparison: "NotEquals",
                    Value: null,
                },
                {
                    $type: "group",
                    Operator: "Or",
                    Children: [
                        {
                            $type: "condition",
                            Field: "LastName",
                            Comparison: "NotEquals",
                            Value: null,
                        },
                        {
                            $type: "condition",
                            Field: QUERY_FIELD_SOURCE,
                            Comparison: "In",
                            Value: DEFAULT_SOURCE_FILTER_VALUES,
                        },
                    ],
                },
            ],
        },
    };
}

export function twoPhaseSubQuery({
                                     include = [],
                                     start = 0,
                                     amount = 1000,
                                 }: {
    include?: string[];
    start?: number;
    amount?: number;
} = {}): QueryConcreteReq {
    return {
        Options: buildOptions(),
        Start: start,
        Amount: amount,
        Include: include,
        whereClause: {
            $type: "condition",
            Field: "IsActive",
            Comparison: "Equals",
            Value: true,
            subClause: {
                $type: "group",
                Operator: "And",
                Children: [
                    {
                        $type: "condition",
                        Field: "FirstName",
                        Comparison: "NotEquals",
                        Value: null,
                    },
                    {
                        $type: "group",
                        Operator: "Or",
                        Children: [
                            {
                                $type: "condition",
                                Field: QUERY_FIELD_SOURCE,
                                Comparison: "NotEquals",
                                Value: null,
                            },
                            {
                                $type: "condition",
                                Field: "LastName",
                                Comparison: "Regex",
                                Value: "^[A-Z][a-z]+$",
                            },
                        ],
                    },
                ],
            },
        },
        SortBy: [{PropertyName: "Id", Direction: "Asc", Priority: 0}],
    };
}

export function heavyIncludeQuery({
                                      include = DEFAULT_PERSON_INCLUDES,
                                      start = 0,
                                      amount = 1998,
                                  }: {
    include?: string[];
    start?: number;
    amount?: number;
} = {}): QueryConcreteReq {
    return {
        Options: buildOptions(),
        Start: start,
        Amount: amount,
        Include: include,
        SortBy: [],
    };
}

export function realisticIncludeQuery({
                                          start = 0,
                                          amount = 200,
                                      }: {
    start?: number;
    amount?: number;
} = {}): QueryConcreteReq {
    return {
        Options: buildOptions(),
        Start: start,
        Amount: amount,
        Include: ["contactaddresses.address"],
        SortBy: [],
    };
}

export function selectProjectionQuery({
                                          start = 0,
                                          amount = 1200,
                                          include = [],
                                          fields = DEFAULT_PERSON_SELECT_FIELDS,
                                      }: {
    start?: number;
    amount?: number;
    include?: string[];
    fields?: string[];
} = {}): ProjectionQueryReq {
    return {
        Options: buildOptions(),
        Start: start,
        Amount: amount,
        Keys: [],
        whereClause: null,
        Include: include,
        Select: fields,
        ComputedFields: [],
        SortBy: [
            {PropertyName: "LastName", Direction: "Asc", Priority: 0},
            {PropertyName: "FirstName", Direction: "Asc", Priority: 1},
        ],
    };
}

export function projectionRootScalarsQuery({
                                               start = 0,
                                               amount = 200,
                                               fields = ["Id", "FirstName", "LastName", "SourceEntityType", "IsActive"],
                                           }: {
    start?: number;
    amount?: number;
    fields?: string[];
} = {}): ProjectionQueryReq {
    return {
        Options: buildOptions(),
        Start: start,
        Amount: amount,
        Keys: [],
        whereClause: null,
        Include: [],
        Select: fields,
        ComputedFields: [],
        SortBy: [
            {PropertyName: "LastName", Direction: "Asc", Priority: 0},
            {PropertyName: "FirstName", Direction: "Asc", Priority: 1},
        ],
    };
}

export function projectionNestedSelectQuery({
                                                start = 0,
                                                amount = 200,
                                                fields = ["Id", "contactaddresses.address.city", "contactaddresses.address.postalcode"],
                                            }: {
    start?: number;
    amount?: number;
    fields?: string[];
} = {}): ProjectionQueryReq {
    return {
        Options: buildOptions(),
        Start: start,
        Amount: amount,
        Keys: [],
        whereClause: null,
        Include: [],
        Select: fields,
        ComputedFields: [],
        SortBy: [
            {PropertyName: "LastName", Direction: "Asc", Priority: 0},
            {PropertyName: "FirstName", Direction: "Asc", Priority: 1},
        ],
    };
}

export function projectionUnifiedCollectionQuery({
                                                     start = 0,
                                                     amount = 200,
                                                     fields = [
                                                         "contactaddresses.id",
                                                         "contactaddresses.address.streettype",
                                                         "contactaddresses.address.streetname",
                                                     ],
                                                     zipSiblingCollectionSelections = true,
                                                 }: {
    start?: number;
    amount?: number;
    fields?: string[];
    zipSiblingCollectionSelections?: boolean | null;
} = {}): ProjectionQueryReq {
    return {
        Options: buildOptions({
            ZipSiblingCollectionSelections: zipSiblingCollectionSelections,
        }),
        Start: start,
        Amount: amount,
        Keys: [],
        whereClause: null,
        Include: [],
        Select: fields,
        ComputedFields: [],
        SortBy: [
            {PropertyName: "LastName", Direction: "Asc", Priority: 0},
            {PropertyName: "FirstName", Direction: "Asc", Priority: 1},
        ],
    };
}

export function computedCollectionParallelQuery({
                                                    start = 0,
                                                    amount = 200,
                                                    name = "streetLine",
                                                    template = "{contactaddresses.address.streettype} {contactaddresses.address.streetname}",
                                                    zipSiblingCollectionSelections = true,
                                                }: {
    start?: number;
    amount?: number;
    name?: string;
    template?: string;
    zipSiblingCollectionSelections?: boolean | null;
} = {}): ProjectionQueryReq {
    return {
        Options: buildOptions({
            ZipSiblingCollectionSelections: zipSiblingCollectionSelections,
        }),
        Start: start,
        Amount: amount,
        Keys: [],
        whereClause: null,
        Include: [],
        Select: ["contactaddresses.id"],
        ComputedFields: [{Name: name, Template: template}],
        SortBy: [
            {PropertyName: "LastName", Direction: "Asc", Priority: 0},
            {PropertyName: "FirstName", Direction: "Asc", Priority: 1},
        ],
    };
}

export function computedScalarTemplateQuery({
                                                start = 0,
                                                amount = 200,
                                                name = "fullName",
                                                template = "{FirstName} {LastName}",
                                            }: {
    start?: number;
    amount?: number;
    name?: string;
    template?: string;
} = {}): ProjectionQueryReq {
    return {
        Options: buildOptions(),
        Start: start,
        Amount: amount,
        Keys: [],
        whereClause: null,
        Include: [],
        Select: ["FirstName", "LastName"],
        ComputedFields: [{Name: name, Template: template}],
        SortBy: [
            {PropertyName: "LastName", Direction: "Asc", Priority: 0},
            {PropertyName: "FirstName", Direction: "Asc", Priority: 1},
        ],
    };
}
