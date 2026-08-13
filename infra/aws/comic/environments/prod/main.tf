terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.80"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Configure remotely before first apply, e.g.:
  # backend "s3" {
  #   bucket         = "lyo-tf-state"
  #   key            = "comic/prod/terraform.tfstate"
  #   region         = "us-east-2"
  #   dynamodb_table = "lyo-tf-locks"
  #   encrypt        = true
  # }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "lyo-comic"
      Environment = "prod"
      ManagedBy   = "terraform"
    }
  }
}

locals {
  ssm_param_prefix = "/${var.name_prefix}/prod"
  web_cidrs        = length(var.web_cidrs) > 0 ? var.web_cidrs : var.admin_cidrs
  postgres_password = coalesce(var.postgres_password, random_password.postgres.result)
}

resource "random_password" "postgres" {
  length  = 32
  special = false
}

data "aws_ami" "al2023" {
  most_recent = true
  owners      = ["amazon"]

  filter {
    name   = "name"
    values = ["al2023-ami-*-x86_64"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

resource "aws_vpc" "comic" {
  cidr_block           = var.vpc_cidr
  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = { Name = "${var.name_prefix}-vpc" }
}

resource "aws_internet_gateway" "comic" {
  vpc_id = aws_vpc.comic.id
  tags   = { Name = "${var.name_prefix}-igw" }
}

resource "aws_subnet" "public" {
  vpc_id                  = aws_vpc.comic.id
  cidr_block              = var.public_subnet_cidr
  availability_zone       = var.availability_zone
  map_public_ip_on_launch = true

  tags = { Name = "${var.name_prefix}-public" }
}

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.comic.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.comic.id
  }

  tags = { Name = "${var.name_prefix}-public-rt" }
}

resource "aws_route_table_association" "public" {
  subnet_id      = aws_subnet.public.id
  route_table_id = aws_route_table.public.id
}

resource "aws_security_group" "app" {
  name        = "${var.name_prefix}-app"
  description = "Comic Next.js + API (admin CIDRs now; web_cidrs can open UI later)"
  vpc_id      = aws_vpc.comic.id

  ingress {
    description = "HTTP"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = local.web_cidrs
  }

  ingress {
    description = "HTTPS"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = local.web_cidrs
  }

  ingress {
    description = "Next.js"
    from_port   = 3101
    to_port     = 3101
    protocol    = "tcp"
    cidr_blocks = local.web_cidrs
  }

  ingress {
    description = "Comic API (admin only; later keep this closed to the world)"
    from_port   = 5000
    to_port     = 5000
    protocol    = "tcp"
    cidr_blocks = var.admin_cidrs
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "${var.name_prefix}-app-sg" }
}

resource "aws_security_group" "db" {
  name        = "${var.name_prefix}-db"
  description = "Postgres host; 5432 from app SG only"
  vpc_id      = aws_vpc.comic.id

  ingress {
    description     = "Postgres from app host"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.app.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "${var.name_prefix}-db-sg" }
}

data "aws_iam_policy_document" "ec2_assume" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["ec2.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "ec2" {
  name               = "${var.name_prefix}-ec2"
  assume_role_policy = data.aws_iam_policy_document.ec2_assume.json
}

resource "aws_iam_role_policy_attachment" "ssm" {
  role       = aws_iam_role.ec2.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

resource "aws_iam_role_policy_attachment" "ecr_read" {
  role       = aws_iam_role.ec2.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryReadOnly"
}

data "aws_iam_policy_document" "ec2_extra" {
  statement {
    sid = "SsmParams"
    actions = [
      "ssm:GetParameter",
      "ssm:GetParameters",
      "ssm:GetParametersByPath",
    ]
    resources = [
      "arn:aws:ssm:${var.aws_region}:*:parameter${local.ssm_param_prefix}/*",
    ]
  }

  statement {
    sid = "S3Backups"
    actions = [
      "s3:PutObject",
      "s3:GetObject",
      "s3:ListBucket",
    ]
    resources = [
      aws_s3_bucket.backups.arn,
      "${aws_s3_bucket.backups.arn}/*",
    ]
  }
}

resource "aws_iam_role_policy" "ec2_extra" {
  name   = "${var.name_prefix}-ec2-extra"
  role   = aws_iam_role.ec2.id
  policy = data.aws_iam_policy_document.ec2_extra.json
}

resource "aws_iam_instance_profile" "ec2" {
  name = "${var.name_prefix}-ec2"
  role = aws_iam_role.ec2.name
}

resource "aws_s3_bucket" "backups" {
  bucket_prefix = "${var.name_prefix}-pg-backups-"
}

resource "aws_s3_bucket_public_access_block" "backups" {
  bucket                  = aws_s3_bucket.backups.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_lifecycle_configuration" "backups" {
  bucket = aws_s3_bucket.backups.id

  rule {
    id     = "expire"
    status = "Enabled"

    filter {}

    expiration {
      days = var.backup_retention_days
    }
  }
}

resource "aws_ssm_parameter" "postgres_db" {
  name  = "${local.ssm_param_prefix}/postgres_db"
  type  = "String"
  value = var.postgres_db
}

resource "aws_ssm_parameter" "postgres_user" {
  name  = "${local.ssm_param_prefix}/postgres_user"
  type  = "String"
  value = var.postgres_user
}

resource "aws_ssm_parameter" "postgres_password" {
  name  = "${local.ssm_param_prefix}/postgres_password"
  type  = "SecureString"
  value = local.postgres_password
}

resource "aws_instance" "db" {
  ami                         = data.aws_ami.al2023.id
  instance_type               = var.db_instance_type
  subnet_id                   = aws_subnet.public.id
  vpc_security_group_ids      = [aws_security_group.db.id]
  iam_instance_profile        = aws_iam_instance_profile.ec2.name
  user_data_replace_on_change = false

  root_block_device {
    volume_size           = var.db_volume_size_gb
    volume_type           = "gp3"
    encrypted             = true
    delete_on_termination = false
  }

  user_data = templatefile("${path.module}/../../templates/db-user-data.sh.tftpl", {
    aws_region            = var.aws_region
    ssm_param_prefix      = local.ssm_param_prefix
    postgres_db           = var.postgres_db
    postgres_user         = var.postgres_user
    backup_bucket_name    = aws_s3_bucket.backups.bucket
    backup_retention_days = var.backup_retention_days
  })

  metadata_options {
    http_tokens                 = "required"
    http_endpoint               = "enabled"
    http_put_response_hop_limit = 2
  }

  tags = {
    Name = "${var.name_prefix}-db"
    Role = "db"
  }

  lifecycle {
    ignore_changes = [ami, user_data]
  }

  depends_on = [aws_ssm_parameter.postgres_password]
}

resource "aws_ssm_parameter" "postgres_host" {
  name  = "${local.ssm_param_prefix}/postgres_host"
  type  = "String"
  value = aws_instance.db.private_ip
}

resource "aws_ssm_parameter" "postgres_connection_string" {
  name = "${local.ssm_param_prefix}/postgres_connection_string"
  type = "SecureString"
  value = join("", [
    "Host=${aws_instance.db.private_ip};",
    "Port=5432;",
    "Database=${var.postgres_db};",
    "Username=${var.postgres_user};",
    "Password=${local.postgres_password}",
  ])
}

data "aws_iam_policy_document" "dlm_assume" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["dlm.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "dlm" {
  name               = "${var.name_prefix}-dlm"
  assume_role_policy = data.aws_iam_policy_document.dlm_assume.json
}

resource "aws_iam_role_policy_attachment" "dlm" {
  role       = aws_iam_role.dlm.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSDataLifecycleManagerServiceRole"
}

resource "aws_dlm_lifecycle_policy" "db_snapshots" {
  description        = "${var.name_prefix} nightly DB EBS snapshots"
  execution_role_arn = aws_iam_role.dlm.arn
  state              = "ENABLED"

  policy_details {
    resource_types = ["INSTANCE"]
    target_tags = {
      Role    = "db"
      Project = "lyo-comic"
    }

    schedule {
      name = "nightly"

      create_rule {
        interval      = 24
        interval_unit = "HOURS"
        times         = ["05:00"]
      }

      retain_rule {
        count = var.backup_retention_days
      }

      tags_to_add = {
        Snapshot = "db-nightly"
      }

      copy_tags = true
    }
  }

  tags = { Name = "${var.name_prefix}-db-dlm" }
}

resource "aws_instance" "app" {
  ami                         = data.aws_ami.al2023.id
  instance_type               = var.app_instance_type
  subnet_id                   = aws_subnet.public.id
  vpc_security_group_ids      = [aws_security_group.app.id]
  iam_instance_profile        = aws_iam_instance_profile.ec2.name
  user_data_replace_on_change = false

  root_block_device {
    volume_size           = var.app_volume_size_gb
    volume_type           = "gp3"
    encrypted             = true
    delete_on_termination = false
  }

  user_data = templatefile("${path.module}/../../templates/app-user-data.sh.tftpl", {
    ssm_param_prefix = local.ssm_param_prefix
  })

  metadata_options {
    http_tokens                 = "required"
    http_endpoint               = "enabled"
    http_put_response_hop_limit = 2
  }

  tags = {
    Name = "${var.name_prefix}-app"
    Role = "app"
  }

  lifecycle {
    ignore_changes = [ami, user_data]
  }

  depends_on = [
    aws_instance.db,
    aws_ssm_parameter.postgres_connection_string,
  ]
}

resource "aws_eip" "app" {
  domain   = "vpc"
  instance = aws_instance.app.id
  tags     = { Name = "${var.name_prefix}-app-eip" }
}

resource "aws_ecr_repository" "api" {
  name                 = "${var.name_prefix}-api"
  image_tag_mutability = "MUTABLE"
  force_delete         = true

  image_scanning_configuration {
    scan_on_push = true
  }
}

resource "aws_ecr_repository" "web" {
  name                 = "${var.name_prefix}-web"
  image_tag_mutability = "MUTABLE"
  force_delete         = true

  image_scanning_configuration {
    scan_on_push = true
  }
}
