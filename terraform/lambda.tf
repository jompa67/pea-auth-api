resource "aws_lambda_function" "api" {
  function_name = local.prefixed_name
  role          = aws_iam_role.lambda_role.arn
  handler       = "${var.project_name}::${var.project_name}.LambdaEntryPoint::FunctionHandlerAsync"
  runtime       = "dotnet8"
  timeout       = 30
  memory_size   = 1024
  filename      = "api.zip"
  source_code_hash = filebase64sha256("api.zip")

  environment {
    variables = {
      ASPNETCORE_ENVIRONMENT        = var.env,
      JwtSettings__Issuer           = var.jwt_issuer,
      JwtSettings__Audience         = var.jwt_audience,
      JwtSettings__PublicKey        = var.jwt_public_key,
      JwtSettings__PrivateKeySecret = aws_secretsmanager_secret.jwt_private_key.arn,
    }
  }
}


resource "aws_iam_role" "lambda_role" {
  name = "${local.prefixed_name}-lambda-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_policy" "access_policy" {
  name = "${local.prefixed_name}-lambda-policy"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "dynamodb:GetItem",
          "dynamodb:PutItem",
          "dynamodb:UpdateItem",
          "dynamodb:DeleteItem",
          "dynamodb:Query",
          "dynamodb:Scan",
          "dynamodb:BatchGetItem",
          "dynamodb:BatchWriteItem"
        ]
        Resource = [
          aws_dynamodb_table.user_refresh_token_table.arn,
          aws_dynamodb_table.users_table.arn,
          aws_dynamodb_table.user_logins_table.arn,
          "${aws_dynamodb_table.user_refresh_token_table.arn}/index/*",
          "${aws_dynamodb_table.users_table.arn}/index/*",
          "${aws_dynamodb_table.user_logins_table.arn}/index/*"
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "kms:Decrypt",
          "kms:DescribeKey"
        ]
        Resource = aws_kms_key.my_key.arn
      },
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue"
        ]
        Resource = aws_secretsmanager_secret.jwt_private_key.arn
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "lambda_basic_execution" {
  role       = aws_iam_role.lambda_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

resource "aws_iam_role_policy_attachment" "access_attachment" {
  role       = aws_iam_role.lambda_role.name
  policy_arn = aws_iam_policy.access_policy.arn
}
