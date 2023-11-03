using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Fluid;
using Fluid.Values;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;
using OrchardCore.Data;
using OrchardCore.Liquid;
using YesSql;

namespace OrchardCore.Queries.Sql
{
    public class SqlQuerySource : IQuerySource
    {
        private readonly ILiquidTemplateManager _liquidTemplateManager;
        private readonly IDbQueryExecutor _queryExecutor;
        private readonly ISession _session;
        private readonly TemplateOptions _templateOptions;

        public SqlQuerySource(
            ILiquidTemplateManager liquidTemplateManager,
            IDbQueryExecutor queryExecutor,
            ISession session,
            IOptions<TemplateOptions> templateOptions)
        {
            _liquidTemplateManager = liquidTemplateManager;
            _queryExecutor = queryExecutor;
            _session = session;
            _templateOptions = templateOptions.Value;
        }

        public string Name => "Sql";

        public Query Create()
        {
            return new SqlQuery();
        }

        public async Task<IQueryResults> ExecuteQueryAsync(Query query, IDictionary<string, object> parameters)
        {
            var sqlQuery = query as SqlQuery;

            var tokenizedQuery = await _liquidTemplateManager.RenderStringAsync(sqlQuery.Template, NullEncoder.Default,
                parameters.Select(x => new KeyValuePair<string, FluidValue>(x.Key, FluidValue.Create(x.Value, _templateOptions))));

            var dialect = _session.Store.Configuration.SqlDialect;
            var sqlQueryResults = new SQLQueryResults();

            if (!SqlParser.TryParse(tokenizedQuery, _session.Store.Configuration.Schema, dialect, _session.Store.Configuration.TablePrefix, parameters, out var rawQuery, out var messages))
            {
                sqlQueryResults.Items = Array.Empty<object>();

                return sqlQueryResults;
            }

            var results = new List<JObject>();

            await _queryExecutor.QueryAsync(async connection =>
            {
                if (sqlQuery.ReturnDocuments)
                {
                    var documentIds = await connection.QueryAsync<long>(rawQuery, parameters);

                    sqlQueryResults.Items = await _session.GetAsync<ContentItem>(documentIds.ToArray());

                    return;
                }

                var queryResults = await connection.QueryAsync(rawQuery, parameters);

                foreach (var document in queryResults)
                {
                    results.Add(JObject.FromObject(document));
                }

                sqlQueryResults.Items = results;
            });

            return sqlQueryResults;
        }
    }
}
