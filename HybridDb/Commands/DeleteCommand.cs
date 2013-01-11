﻿using System;
using System.Collections.Generic;
using Dapper;
using HybridDb.Schema;

namespace HybridDb.Commands
{
    public class DeleteCommand : DatabaseCommand
    {
        readonly Guid currentEtag;
        readonly bool lastWriteWins;
        readonly Guid key;
        readonly ITable table;

        public DeleteCommand(ITable table, Guid key, Guid etag, bool lastWriteWins)
        {
            this.table = table;
            this.key = key;
            currentEtag = etag;
            this.lastWriteWins = lastWriteWins;
        }

        internal override PreparedDatabaseCommand Prepare(DocumentStore store, Guid etag, int uniqueParameterIdentifier)
        {
            var sql = new SqlBuilder()
                .Append("delete from {0} where {1} = @Id{2}",
                        store.Escape(store.GetFormattedTableName(table)),
                        table.IdColumn.Name,
                        uniqueParameterIdentifier)
                .Append(!lastWriteWins,
                        "and {0} = @CurrentEtag{1}",
                        table.EtagColumn.Name,
                        uniqueParameterIdentifier)
                .ToString();

            var parameters = new List<Parameter>();
            parameters.Add(new Parameter {Name = "@Id" + uniqueParameterIdentifier, Value = key, DbType = table.IdColumn.Column.DbType});
            if (!lastWriteWins)
            {
                parameters.Add(new Parameter {Name = "@CurrentEtag" + uniqueParameterIdentifier, Value = currentEtag, DbType = table.EtagColumn.Column.DbType});
            }

            return new PreparedDatabaseCommand
            {
                Sql = sql,
                Parameters = parameters,
                ExpectedRowCount = 1
            };
        }
    }
}