terraform {
  required_version = ">= 1.9.0"
}

variable "environment_name" {
  type        = string
  description = "Deployment environment name."
  default     = "staging"

  validation {
    condition     = var.environment_name == "staging"
    error_message = "This environment contract is only for staging."
  }
}

variable "customer_data_mode" {
  type        = string
  description = "Allowed customer data posture for staging."
  default     = "synthetic-only"

  validation {
    condition     = var.customer_data_mode == "synthetic-only"
    error_message = "Staging must use synthetic-only data and must not contain production customer data."
  }
}

locals {
  data_posture = "No-CUI / compliance management only"

  staging_services = {
    api = {
      purpose       = "ASP.NET Core API"
      health_signal = "/health service gccs-api"
    }
    web = {
      purpose       = "React/Vite web app"
      health_signal = "static asset deployment"
    }
    database = {
      purpose       = "PostgreSQL staging database"
      health_signal = "postgresql"
    }
    object_storage = {
      purpose       = "Evidence and contract document object storage"
      health_signal = "object-storage"
    }
    cache = {
      purpose       = "Redis cache and coordination"
      health_signal = "redis"
    }
    queue = {
      purpose       = "Background job queue"
      health_signal = "background-jobs"
    }
    secrets = {
      purpose       = "Managed secret store for staging-only values"
      health_signal = "deployment secret resolution"
    }
  }

  operational_controls = {
    migrations = "CI generates and applies an idempotent EF Core migration script before smoke tests."
    logs       = "API, web, migration, and background job logs are routed to the staging log workspace."
    alerts     = "Basic alerts cover API health, dependency degradation, queue backlog, and job failures."
    smoke      = "CI calls /health and checks API, database, cache, storage, and background job signals."
  }
}

output "environment_name" {
  value = var.environment_name
}

output "customer_data_mode" {
  value = var.customer_data_mode
}

output "data_posture" {
  value = local.data_posture
}

output "staging_services" {
  value = local.staging_services
}

output "operational_controls" {
  value = local.operational_controls
}
