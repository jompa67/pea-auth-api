locals {
  region        = "eu-east-1"
  prefixed_name = "${var.prefix}-${var.service_name}"
  tags = {
    Environment = var.env
    Service     = var.service_name
    ManagedBy   = "terraform"
  }
}