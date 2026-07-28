output "web_public_ip" {
  description = "Elastic IP of the Next.js web EC2."
  value       = aws_eip.web.public_ip
}

output "web_public_dns" {
  description = "Public DNS of the web EC2."
  value       = aws_instance.web.public_dns
}

output "api_public_ip" {
  description = "Elastic IP of the API EC2 (admin only; web uses private IP)."
  value       = aws_eip.api.public_ip
}

output "api_private_ip" {
  description = "Private IP for LYO_API_BASE_URL on the web host."
  value       = aws_instance.api.private_ip
}

output "lyo_api_base_url" {
  description = "Internal TestApi URL for the web compose stack."
  value       = "http://${aws_instance.api.private_ip}:5251"
}

output "ecr_testapi_url" {
  value = aws_ecr_repository.testapi.repository_url
}

output "ecr_web_url" {
  value = aws_ecr_repository.web.repository_url
}

output "ssm_postgres_password_name" {
  value = aws_ssm_parameter.postgres_password.name
}

output "web_instance_id" {
  value = aws_instance.web.id
}

output "api_instance_id" {
  value = aws_instance.api.id
}
