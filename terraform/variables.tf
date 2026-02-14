variable "region" {
  description = "AWS Region"
  type        = string
}

variable "prefix" {
  description = "Prefix for the resources"
  type        = string
  default     = "jm"
}

variable "service_name" {
  description = "Application that we want to deploy"
  type        = string
  default     = "basket-api"
}

variable "env" {
  description = "Application env"
  type        = string
  default     = "dev"
}

variable "api_methods" {
  type = list(string)
  default = ["GET", "POST", "PUT", "DELETE"]
}

variable "stage_name" {
  description = "Stage name for the API Gateway"
  type        = string
  default     = "live"
}

variable "deployment_description" {
  description = "Description for the deployment"
  type        = string
  default     = "Deployment of the api"
}

variable "api_key" {
  description = "API Key"
  type        = string
  sensitive   = true
}

variable "domain_name" {
  description = "Domain name for the API Gateway"
  type        = string
  default     = "api.jm.maxlabs.se"
}

variable "base_path" {
  description = "Base path mapping for the API Gateway"
  type        = string
}

variable "project_name" {
  description = "Project name"
  type        = string
}

variable "jwt_issuer" {
  description = "JWT Issuer"
  type        = string
}

variable "jwt_audience" {
  description = "JWT Audience"
  type        = string
}

variable "jwt_public_key" {
  description = "JWT Public Key"
  type        = string
}

variable "jwt_private_key" {
  description = "JWT Private Key"
  type        = string
  sensitive   = true
}

variable "smtp_server" {
  description = "SMTP Server"
  type        = string
}

variable "smtp_port" {
  description = "SMTP Port"
  type        = number
  default     = 587
}

variable "smtp_username" {
  description = "SMTP Username"
  type        = string
}

variable "smtp_password" {
  description = "SMTP Password"
  type        = string
  sensitive   = true
}

variable "email_sender_email" {
  description = "Sender Email"
  type        = string
}

variable "email_sender_name" {
  description = "Sender Name"
  type        = string
}

variable "email_base_url" {
  description = "Base URL for emails"
  type        = string
}
