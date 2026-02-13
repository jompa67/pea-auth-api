resource "aws_kms_key" "my_key" {
  description             = "KMS key for ${local.prefixed_name}"
  deletion_window_in_days = 10
  enable_key_rotation     = true

  tags = {
    Environment = var.env
  }
}

resource "aws_kms_alias" "my_key_alias" {
  name          = "alias/${local.prefixed_name}-key"
  target_key_id = aws_kms_key.my_key.key_id
  lifecycle {
      prevent_destroy = true
      }
}
