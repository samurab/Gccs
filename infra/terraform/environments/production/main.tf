terraform {
  required_version = ">= 1.9.0"
}

variable "environment_name" {
  type        = string
  description = "Deployment environment name."
  default     = "production"

  validation {
    condition     = var.environment_name == "production"
    error_message = "This environment contract is only for production."
  }
}

variable "customer_data_mode" {
  type        = string
  description = "Allowed customer data posture for production."
  default     = "no-cui-only"

  validation {
    condition     = var.customer_data_mode == "no-cui-only"
    error_message = "Production must remain No-CUI only and must not accept real CUI."
  }
}

locals {
  data_posture = "No-CUI / compliance management only"

  production_services = {
    api = {
      purpose       = "ASP.NET Core API"
      health_signal = "/health service gccs-api"
    }
    web = {
      purpose       = "React/Vite web app"
      health_signal = "static asset deployment"
    }
    database = {
      purpose       = "PostgreSQL production database"
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
      purpose       = "Managed secret store for production-only values"
      health_signal = "deployment secret resolution"
    }
    background_jobs = {
      purpose       = "Scheduled and queued operational work"
      health_signal = "background-jobs"
    }
  }

  operational_controls = {
    migrations    = "CI generates and applies an idempotent EF Core migration script before production health checks."
    health_checks = "CI calls /health and checks API, database, cache, storage, and background job signals."
    logs          = "API, web, migration, upload/storage, queue, and background job logs route to the production log workspace."
    alerts        = "Production alerts cover API health, dependency degradation, queue backlog, job failures, upload failures, and migration failure."
    rollback      = "Rollback uses the previously approved launch candidate artifact unless a database restore or forward fix is required."
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

output "production_services" {
  value = local.production_services
}

output "operational_controls" {
  value = local.operational_controls
}
