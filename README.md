# Database MCP Server

A Model Context Protocol (MCP) server that provides AI agents with safe, structured access to SQL Server databases.

## Features

- **Schema Introspection**: Get table structures, column information, and relationships
- **Safe Query Execution**: Execute read-only SELECT queries with built-in safety checks
- **Sample Data Access**: Retrieve sample data from tables for context
- **Stored Procedure Discovery**: List and examine stored procedures
- **Security-First Design**: Prevents unsafe operations (INSERT, UPDATE, DELETE)

## Available Tools

1. `list_tables` - List all tables in the database
2. `get_table_schema` - Get detailed schema information for a specific table
3. `get_sample_data` - Retrieve sample data from a table
4. `list_stored_procedures` - List all stored procedures
5. `execute_query` - Execute read-only SELECT queries

## Configuration

Update `appsettings.json` with your database connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your_server;Database=your_db;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

## Running the Server

```bash
dotnet build
dotnet run
```

## Security Features

- Only SELECT queries are allowed
- Query results are limited to prevent excessive data retrieval
- Connection timeouts prevent long-running operations
- Input validation prevents SQL injection

## Usage with AI Clients

Configure your AI client (like Claude Desktop) to connect to this MCP server for database analysis and querying capabilities.

## Requirements

- .NET 8.0 or higher
- SQL Server database
- Appropriate database permissions for read access