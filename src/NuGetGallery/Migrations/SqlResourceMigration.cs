// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Data.Entity.Migrations;
using System.IO;

namespace NuGetGallery.Migrations
{
    public class SqlResourceMigration : DbMigration
    {
        private static readonly string[] Go = new[] { "GO" };
        private readonly string[] _sqlFiles;

        public SqlResourceMigration(string embeddedResourceSqlFile)
        {
            _sqlFiles = new [] { embeddedResourceSqlFile };
        }

        public SqlResourceMigration(string[] embeddedResourceSqlFiles)
        {
            _sqlFiles = embeddedResourceSqlFiles;
        }

        public override void Up()
        {
            foreach (var sqlFile in _sqlFiles)
            {
                ExecuteEmbeddedSql(sqlFile);
            }
        }

        private void ExecuteEmbeddedSql(string sqlFile)
        {
            Stream stream = typeof(ExecuteELMAHSql).Assembly.GetManifestResourceStream(sqlFile);
            using (var streamReader = new StreamReader(stream))
            {
                var statements = streamReader.ReadToEnd().Split(Go, StringSplitOptions.RemoveEmptyEntries);

                foreach (var statement in statements)
                {
                    if (String.IsNullOrWhiteSpace(statement))
                    {
                        continue;
                    }

                    Sql(statement);
                }
            }
        }

        public override void Down()
        {
        }
    }
}