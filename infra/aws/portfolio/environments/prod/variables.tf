variable "aws_region" {
  type        = string
  description = "AWS region for the portfolio stack."
  default     = "us-east-2"
}

variable "name_prefix" {
  type        = string
  description = "Prefix for resource names."
  default     = "lyo-portfolio"
}

variable "vpc_cidr" {
  type    = string
  default = "10.40.0.0/16"
}

variable "public_subnet_cidr" {
  type    = string
  default = "10.40.1.0/24"
}

variable "availability_zone" {
  type        = string
  description = "AZ for the single public subnet (e.g. us-east-2a)."
  default     = "us-east-2a"
}

variable "web_instance_type" {
  type    = string
  default = "t3.small"
}

variable "api_instance_type" {
  type    = string
  default = "t3.medium"
}

variable "admin_cidrs" {
  type        = list(string)
  description = "CIDRs allowed to hit Portfolio API:5251 and Next.js:3100 directly (SSM preferred for shell)."
  default     = []
}

variable "postgres_password" {
  type        = string
  description = "Postgres password stored in SSM (set via TF_VAR_postgres_password or tfvars; never commit)."
  sensitive   = true
}

variable "rabbitmq_password" {
  type        = string
  description = "RabbitMQ password stored in SSM."
  sensitive   = true
  default     = "lyo"
}
