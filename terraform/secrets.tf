resource "aws_secretsmanager_secret" "jwt_private_key" {
  name                    = "${local.prefixed_name}-jwt-private-key"
  description             = "JWT Private Key for ${local.prefixed_name}"
  recovery_window_in_days = 7

  tags = {
    Environment = var.env
  }
}

resource "aws_secretsmanager_secret_version" "jwt_private_key_version" {
  secret_id     = aws_secretsmanager_secret.jwt_private_key.id
  secret_string = var.jwt_private_key
}
