provider azurerm {
  version = "~>2.0.0"
  features{}
}

variable "name" {
  type = "string"
}

variable "location" {
  type = "string"
  default = "centralus"
}

resource "azurerm_resource_group" "rg" {
  name     = "${var.name}-rg"
  location = var.location
}

resource "azurerm_cosmosdb"