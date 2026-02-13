data "aws_region" "current" {}
data "aws_caller_identity" "current" {}


resource "aws_api_gateway_rest_api" "api" {
  name        = local.prefixed_name
  description = "API Gateway for ${local.prefixed_name}"
}

####################################################################################
###############################  BASE-PATH MAPPING  ################################
####################################################################################

resource "aws_api_gateway_base_path_mapping" "base_path_mapping" {
  depends_on = [aws_api_gateway_stage.live_stage]
  api_id      = aws_api_gateway_rest_api.api.id
  stage_name  = aws_api_gateway_stage.live_stage.stage_name
  domain_name = var.domain_name
  base_path   = var.base_path
}

####################################################################################
###############################  API GW METHODS  ###################################
####################################################################################

resource "aws_api_gateway_resource" "api_base" {
  rest_api_id = aws_api_gateway_rest_api.api.id
  parent_id   = aws_api_gateway_rest_api.api.root_resource_id
  path_part   = "api"
}

resource "aws_api_gateway_resource" "swagger_base" {
  rest_api_id = aws_api_gateway_rest_api.api.id
  parent_id   = aws_api_gateway_rest_api.api.root_resource_id
  path_part   = "swagger"
}

resource "aws_api_gateway_resource" "proxy_api" {
  rest_api_id = aws_api_gateway_rest_api.api.id
  parent_id   = aws_api_gateway_resource.api_base.id
  path_part   = "{proxy+}"
}

resource "aws_api_gateway_resource" "proxy_swagger" {
  rest_api_id = aws_api_gateway_rest_api.api.id
  parent_id   = aws_api_gateway_resource.swagger_base.id
  path_part   = "{proxy+}"
}

####################################################################################
################################  PERMISSIONS  #####################################
####################################################################################

resource "aws_lambda_permission" "api_permission" {
  statement_id  = "AllowExecutionFromApiGatewayAPI"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.api.function_name
  principal     = "apigateway.amazonaws.com"

  source_arn = "arn:aws:execute-api:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}:${aws_api_gateway_rest_api.api.id}/*/*/api/*"
}

resource "aws_lambda_permission" "swagger_permission" {
  statement_id  = "AllowExecutionFromApiGatewaySwagger"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.api.function_name
  principal     = "apigateway.amazonaws.com"

  source_arn = "arn:aws:execute-api:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}:${aws_api_gateway_rest_api.api.id}/*/*/swagger/*"
}

####################################################################################
###############################  API GW METHODS  ###################################
####################################################################################

resource "aws_api_gateway_method" "swagger_get" {
  rest_api_id      = aws_api_gateway_rest_api.api.id
  resource_id      = aws_api_gateway_resource.proxy_swagger.id
  http_method      = "GET"
  authorization    = "NONE"
  api_key_required = false
  request_parameters = {
    "method.request.header.User-Agent" = true
  }
}

resource "aws_api_gateway_method" "api_methods" {
  for_each = toset(var.api_methods)
  rest_api_id      = aws_api_gateway_rest_api.api.id
  resource_id      = aws_api_gateway_resource.proxy_api.id
  http_method      = each.value
  authorization    = "NONE"
  api_key_required = false
  request_parameters = {
    "method.request.header.x-client"         = true
    "method.request.header.x-correlation-id" = true
    "method.request.path.proxy"              = true
    "method.request.header.Origin"           = true
  }
}

resource "aws_api_gateway_integration" "api_integrations" {
  for_each = toset(var.api_methods)
  rest_api_id             = aws_api_gateway_rest_api.api.id
  resource_id             = aws_api_gateway_resource.proxy_api.id
  http_method             = each.value
  integration_http_method = "POST"
  type                    = "AWS_PROXY"
  uri                     = aws_lambda_function.api.invoke_arn

  depends_on = [aws_api_gateway_method.api_methods]
}

resource "aws_api_gateway_integration" "swagger_get_integration" {
  rest_api_id             = aws_api_gateway_rest_api.api.id
  resource_id             = aws_api_gateway_resource.proxy_swagger.id
  http_method             = aws_api_gateway_method.swagger_get.http_method
  integration_http_method = "POST"
  type                    = "AWS_PROXY"
  uri                     = aws_lambda_function.api.invoke_arn
}

####################################################################################
################################  USAGE PLAN  ######################################
####################################################################################

resource "aws_api_gateway_usage_plan" "api_usage_plan" {
  name        = "${local.prefixed_name}-cloudfront_only"
  description = "Cloudfront usage plan"

  api_stages {
    api_id = aws_api_gateway_rest_api.api.id
    stage  = aws_api_gateway_stage.live_stage.stage_name
  }

  quota_settings {
    limit  = 500000
    period = "MONTH"
  }

  throttle_settings {
    burst_limit = 200
    rate_limit  = 100
  }
}

resource "aws_api_gateway_api_key" "api_key" {
  name        = "${local.prefixed_name}-key"
  description = "Cloudfront API Key"
  enabled     = true
  value       = var.api_key
  lifecycle {
      prevent_destroy = true
      }
}

resource "aws_api_gateway_usage_plan_key" "api_usage_plan_key" {
  key_id        = aws_api_gateway_api_key.api_key.id
  key_type      = "API_KEY"
  usage_plan_id = aws_api_gateway_usage_plan.api_usage_plan.id
}


####################################################################################
################################  DEPLOYMENT  ######################################
####################################################################################

resource "aws_api_gateway_stage" "live_stage" {
  stage_name    = var.stage_name
  rest_api_id   = aws_api_gateway_rest_api.api.id
  deployment_id = aws_api_gateway_deployment.api.id
  description   = "Live stage - ${var.deployment_description}"

  xray_tracing_enabled  = false
  cache_cluster_enabled = false
  cache_cluster_size    = "0.5"
}

resource "aws_api_gateway_deployment" "api" {
  rest_api_id = aws_api_gateway_rest_api.api.id

  triggers = {
    redeployment = sha256(jsonencode([
      aws_api_gateway_method.api_methods,
      aws_api_gateway_method.swagger_get,
      aws_api_gateway_integration.api_integrations
    ]))
  }

  lifecycle {
    create_before_destroy = true
  }
}
