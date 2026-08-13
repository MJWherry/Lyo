output "app_public_ip" {
  description = "Elastic IP of the combined UI + API EC2."
  value       = aws_eip.app.public_ip
}

output "app_public_dns" {
  value = aws_instance.app.public_dns
}

output "app_instance_id" {
  value = aws_instance.app.id
}

output "db_instance_id" {
  value = aws_instance.db.id
}

output "db_private_ip" {
  description = "Private IP for Postgres (app compose POSTGRES_HOST)."
  value       = aws_instance.db.private_ip
}

output "lyo_comic_api_base_url" {
  description = "In-compose Comic API URL (web → api on the docker network)."
  value       = "http://api:5000"
}

output "lyo_comic_public_auth_url" {
  description = "Browser-facing Comic API origin for Google OIDC until Caddy fronts /auth."
  value       = "http://${aws_eip.app.public_ip}:5000"
}

output "lyo_comic_web_url" {
  value = "http://${aws_eip.app.public_ip}:3101"
}

output "ecr_api_url" {
  value = aws_ecr_repository.api.repository_url
}

output "ecr_web_url" {
  value = aws_ecr_repository.web.repository_url
}

output "ssm_param_prefix" {
  value = local.ssm_param_prefix
}

output "backup_bucket" {
  value = aws_s3_bucket.backups.bucket
}
