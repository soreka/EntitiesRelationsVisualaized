Apologies for the confusion. Here is the entire README content without breaking it into different formats or parts, all in one go:

```markdown
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
   ```

2. **Install Dependencies**:
   - Ensure that the MSAGL library is referenced in your project. You can install it via NuGet:
     ```bash
     Install-Package Microsoft.Msagl -Version x.x.x
     ```

3. **Configure the Database Connection**:
   - Update the `connectionString` in `Program.cs` with your SQL Server connection details:
     ```csharp
     string connectionString = "Data Source=(localdb)\\myWorkSpace;Initial Catalog=AdventureWorks2016;User ID=soreka;Password=soreka123";
     ```

4. **Build and Run the Project**:
   - Open the project in Visual Studio or your preferred .NET IDE.
   - Build the solution and run the project. The UI should display the generated graph based on your database schema.

## Usage

- **Viewing Relationships**: The graph will automatically render based on the relationships detected in your database. Nodes represent tables, and edges represent relationships.
- **Hover for Details**: Hover over an edge in the graph to see detailed information about the relationship between the connected tables.
- **Zoom and Pan**: Use your mouse or touchpad to zoom in/out and pan around the graph for better visibility.

## Code Structure

### `Program.cs`
- The entry point of the application. Initializes the form and graph viewer, and orchestrates the graph-building process.

### `DatabaseManager.cs`
- Handles database connections and SQL query execution. This component abstracts database interactions, making it easy to execute queries and retrieve results.

### `GraphManager.cs`
- Manages the creation and customization of the graph. It handles the retrieval of relationships and primary keys from the database and constructs nodes and edges for the graph.

### `UIManager.cs`
- Manages the user interface and interaction with the graph viewer. Handles events like mouse movements to display relationship details dynamically.

### `Table.cs` and `TableRelationship.cs`
- Data models representing database tables and their relationships. These classes encapsulate information about the tables and relationships used to build the graph.

## Customization

- **Relationship Descriptions**: You can modify the relationship descriptions in the SQL query within `GraphManager.cs` to match your specific requirements.
- **Graph Appearance**: Customize the appearance of nodes and edges in the `DrawNode` and `CreateEdge` methods to suit your visual preferences.

## Contributions

Contributions are welcome! If you find a bug or have a feature request, please create an issue or submit a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

## Contact

For any inquiries or support, please contact `ahmed.m.tayah@gmail.com`.
```
