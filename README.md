# Database Schema Visualization Tool

## Overview

This project is a **Database Schema Visualization Tool** that dynamically generates a visual representation of database relationships (such as one-to-many, many-to-many, etc.) based on the structure of a given SQL Server database. The tool provides an intuitive UI for viewing these relationships, enabling users to better understand the connections between different tables in the database.

## Features

- **Automatic Relationship Detection**: Identifies and categorizes relationships (e.g., one-to-one, one-to-many, many-to-many) between tables in the database.
- **Custom Graph Visualization**: Renders the database schema as a graph, with nodes representing tables and edges representing relationships.
- **Interactive UI**: Allows users to hover over edges to view relationship details.
- **Primary Key Highlighting**: Displays primary keys for each table within the graph nodes.

## Prerequisites

- **.NET Framework**: This project is built using the .NET Framework. Ensure you have a compatible version installed.
- **Microsoft SQL Server**: The tool is designed to work with Microsoft SQL Server databases. Ensure you have access to a database instance.
- **MSAGL Library**: The Microsoft Automatic Graph Layout (MSAGL) library is used for graph rendering. Ensure the necessary dependencies are installed.

## Setup Instructions

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/yourusername/DatabaseSchemaVisualizationTool.git
   cd DatabaseSchemaVisualizationTool
