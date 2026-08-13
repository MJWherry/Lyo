variable "aws_region" {
  type        = string
  description = "AWS region for the comic stack."
  default     = "us-east-2"
}

variable "name_prefix" {
  type        = string
  description = "Prefix for resource names."
  default     = "lyo-comic"
}

variable "vpc_cidr" {
  type    = string
  default = "10.41.0.0/16"
}

variable "public_subnet_cidr" {
  type    = string
  default = "10.41.1.0/24"
}

variable "availability_zone" {
  type        = string
  description = "AZ for the single public subnet (e.g. us-east-2a)."
  default     = "us-east-2a"
}

variable "app_instance_type" {
  type        = string
  description = "Combined Next.js + Comic API host."
  default     = "t3.medium"
}

variable "db_instance_type" {
  type    = string
  default = "t3.small"
}

variable "db_volume_size_gb" {
  type    = number
  default = 50
}

variable "app_volume_size_gb" {
  type    = number
  default = 40
}

variable "admin_cidrs" {
  type        = list(string)
  description = "CIDRs allowed to reach the app host (UI + API). API :5000 stays on this list even if web_cidrs is opened later."
  default     = ["24.3.30.20/32"]
}

variable "web_cidrs" {
  type        = list(string)
  description = "CIDRs for 80/443/3101. Empty = same as admin_cidrs. Set to [\"0.0.0.0/0\"] later to open the UI; keep admin_cidrs on :5000."
  default     = []
}

variable "postgres_db" {
  type    = string
  default = "lyo"
}

variable "postgres_user" {
  type    = string
  default = "lyo"
}

variable "postgres_password" {
  type        = string
  description = "Postgres password (TF_VAR or tfvars). Null generates one."
  default     = null
  sensitive   = true
}

variable "backup_retention_days" {
  type    = number
  default = 7
}
