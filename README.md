# BaseCSharpServerSolution

For new controllers in your project extend BaseModelController, BaseOwnedModelController, BaseAuthModelController, or BaseAccountController.
For new sql contexts in your project extend TemplateMicrosoftSQLDBContext and TemplateDesignTimeContextFactory<T> where T is your extended TemplateMicrosoftSQLDBContext class


For migrations to your db contexts:
dotnet ef migrations add ${input:MigrationName} - workingDirectory: Where you put your new sql context project
dotnet ef database update - workingDirectory: Where you put your new sql context project
dotnet ef migrations script ${input:FullMigrationName} > Scripts/${input:MigrationName}.sql - workingDirectory: Where you put your new sql context project
