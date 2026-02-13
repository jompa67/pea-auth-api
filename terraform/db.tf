# Users Table (User Profiles)
resource "aws_dynamodb_table" "users_table" {
  name         = "${var.prefix}-users"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "id"

  attribute {
    name = "id"
    type = "S"
  }

  attribute {
    name = "username"
    type = "S"
  }

  attribute {
    name = "email"
    type = "S"
  }

  global_secondary_index {
    name            = "UsernameIndex"
    hash_key        = "username"
    projection_type = "ALL"
  }

  global_secondary_index {
    name            = "EmailIndex"
    hash_key        = "email"
    projection_type = "ALL"
  }

  tags = {
    Environment = var.env
  }
}

# User Logins Table (Authentication)
resource "aws_dynamodb_table" "user_logins_table" {
  name         = "${var.prefix}-user-logins"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "user_id"
  range_key    = "auth_provider"

  attribute {
    name = "user_id"
    type = "S"
  }

  attribute {
    name = "auth_provider"
    type = "S"
  }

  tags = {
    Environment = var.env
  }
}

# User RefreshToken Table (Authentication)
resource "aws_dynamodb_table" "user_refresh_token_table" {
  name         = "${var.prefix}-refresh-token"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "RefreshToken"

  attribute {
    name = "RefreshToken"
    type = "S"
  }

  attribute {
    name = "Token"
    type = "S"
  }

  global_secondary_index {
    name            = "TokenIndex"
    hash_key        = "Token"
    projection_type = "ALL"
  }

  tags = {
    Environment = var.env
  }
}
