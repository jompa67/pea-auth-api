output "api_gateway_url" {
  description = "URL of the API Gateway"
  value       = "https://${aws_api_gateway_rest_api.api.id}.execute-api.${data.aws_region.current.name}.amazonaws.com/${aws_api_gateway_stage.live_stage.stage_name}"
}

output "api_gateway_id" {
  description = "ID of the API Gateway"
  value       = aws_api_gateway_rest_api.api.id
}

output "api_gateway_stage" {
  description = "Stage name of the API Gateway"
  value       = aws_api_gateway_stage.live_stage.stage_name
}

output "lambda_function_name" {
  description = "Name of the Lambda function"
  value       = aws_lambda_function.api.function_name
}

output "jwt_private_key_secret_arn" {
  description = "ARN of the JWT private key secret in Secrets Manager"
  value       = aws_secretsmanager_secret.jwt_private_key.arn
  sensitive   = true
}
