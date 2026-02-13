locals {
  region        = var.region
  prefixed_name = "${var.prefix}-${var.service_name}"
  tags = {
    Environment = var.env
    Service     = var.service_name
    ManagedBy   = "terraform"
  }
}