﻿/*
CRM PowerShell Library
Copyright (C) 2017 Arjan Meskers / AMSoftware

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Linq;
using System.Management.Automation;
using AMSoftware.Crm.PowerShell.Common.Repositories;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace AMSoftware.Crm.PowerShell.Commands.Plugins
{
    [Cmdlet(VerbsCommon.Get, "ServiceEndpoint", HelpUri = HelpUrlConstants.GetServiceEndpointHelpUrl, SupportsPaging = true, DefaultParameterSetName = GetServiceEndpointByFilterParameterSet)]
    [OutputType(typeof(Entity))]
    public sealed class GetServiceEndpointCommand : CrmOrganizationCmdlet
    {
        private const string GetServiceEndpointByNameParameterSet = "GetServiceEndPointByName";
        private const string GetServiceEndpointByIdParameterSet = "GetServiceEndPointById";
        private const string GetServiceEndpointByFilterParameterSet = "GetServiceEndPointByFilter";

        private ContentRepository _repository = new ContentRepository();

        [Parameter(Position = 1, Mandatory = true, ParameterSetName = GetServiceEndpointByIdParameterSet)]
        [ValidateNotNull]
        public Guid Id { get; set; }

        [Parameter(Position = 1, Mandatory = true, ParameterSetName = GetServiceEndpointByNameParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter(ParameterSetName = GetServiceEndpointByFilterParameterSet)]
        [ValidateNotNullOrEmpty]
        [SupportsWildcards]
        public string Include { get; set; }

        [Parameter(ParameterSetName = GetServiceEndpointByFilterParameterSet)]
        [ValidateNotNullOrEmpty]
        [SupportsWildcards]
        public string Exclude { get; set; }

        protected override void ExecuteCmdlet()
        {
            base.ExecuteCmdlet();

            switch (this.ParameterSetName)
            {
                case GetServiceEndpointByNameParameterSet:
                    QueryExpression nameQuery = BuildServiceEndpointByNameQuery();
                    GetContentByQuery(nameQuery);
                    break;
                case GetServiceEndpointByIdParameterSet:
                    WriteObject(_repository.Get("serviceendpoint", Id));
                    break;
                case GetServiceEndpointByFilterParameterSet:
                    GetFilteredContent();
                    break;
                default:
                    break;
            }
        }

        private void GetFilteredContent()
        {
            QueryExpression advancedFilterQuery = new QueryExpression("serviceendpoint")
            {
                ColumnSet = new ColumnSet(true)
            };

            if (PagingParameters.IncludeTotalCount)
            {
                double accuracy;
                int count = _repository.GetRowCount(advancedFilterQuery, out accuracy);
                WriteObject(PagingParameters.NewTotalCount(Convert.ToUInt64(count), accuracy));
            }

            var result = _repository.Get(advancedFilterQuery, PagingParameters.First, PagingParameters.Skip);
            if (!string.IsNullOrWhiteSpace(Include))
            {
                WildcardPattern includePattern = new WildcardPattern(Include, WildcardOptions.IgnoreCase);
                result = result.Where(a => includePattern.IsMatch(a.GetAttributeValue<string>("name")) || includePattern.IsMatch(a.GetAttributeValue<string>("solutionnamespace")));
            }
            if (!string.IsNullOrWhiteSpace(Exclude))
            {
                WildcardPattern excludePattern = new WildcardPattern(Exclude, WildcardOptions.IgnoreCase);
                result = result.Where(a => !(excludePattern.IsMatch(a.GetAttributeValue<string>("name")) || excludePattern.IsMatch(a.GetAttributeValue<string>("solutionnamespace"))));
            }

            WriteObject(result, true);
        }

        private void GetContentByQuery(QueryBase query)
        {
            if (PagingParameters.IncludeTotalCount)
            {
                double accuracy;
                int count = _repository.GetRowCount(query, out accuracy);
                WriteObject(PagingParameters.NewTotalCount(Convert.ToUInt64(count), accuracy));
            }

            foreach (var item in _repository.Get(query, PagingParameters.First, PagingParameters.Skip))
            {
                WriteObject(item);
            }
        }

        private QueryExpression BuildServiceEndpointByNameQuery()
        {
            QueryExpression query = new QueryExpression("serviceendpoint")
            {
                ColumnSet = new ColumnSet(true),
                Criteria =
                {
                    Filters = {
                        new FilterExpression(LogicalOperator.Or)
                        {
                            Conditions =
                            {
                                new ConditionExpression("name", ConditionOperator.Equal, Name),
                                new ConditionExpression("solutionnamespace", ConditionOperator.Equal, Name)
                            }
                        }
                    }
                }
            };
            return query;
        }
    }
}
