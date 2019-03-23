﻿using System;
using System.Collections.Generic;
using System.Linq;
using CaptainHook.Common;
using CaptainHook.Common.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CaptainHook.EventHandlerActorService.Handlers
{
    public class RequestBuilder : IRequestBuilder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public string BuildUri(WebhookConfig config, string payload)
        {
            var uri = config.Uri;
            //build the uri from the routes first
            var routingRules = config.WebhookRequestRules.FirstOrDefault(l => l.Routes.Any());
            if (routingRules != null)
            {
                if (routingRules.Source.Location == Location.Body)
                {
                    var path = routingRules.Source.Path;
                    var value = ModelParser.ParsePayloadPropertyAsString(path, payload);

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentNullException(nameof(path), "routing path value in message payload is null or empty");
                    }

                    //selects the route based on the value found in the payload of the message
                    WebhookConfigRoute route = null;
                    foreach (var rules in config.WebhookRequestRules.Where(r => r.Routes.Any()))
                    {
                        route = rules.Routes.FirstOrDefault(r => r.Selector.Equals(value, StringComparison.OrdinalIgnoreCase));
                        if (route != null)
                        {
                            break;
                        }
                    }

                    if (route != null)
                    {
                        uri = route.Uri;
                    }
                }
            }

            //after route has been selected then select the identifier for the RESTful URI if applicable
            var uriRules = config.WebhookRequestRules.FirstOrDefault(l => l.Destination.Location == Location.Uri);
            if (uriRules == null)
            {
                return uri;
            }

            if (uriRules.Source.Location != Location.Body)
            {
                return uri;
            }

            var parameter = ModelParser.ParsePayloadPropertyAsString(uriRules.Source.Path, payload);
            uri = CombineUriAndResourceId(uri, parameter);
            return uri;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private static string CombineUriAndResourceId(string uri, string parameter)
        {
            var position = uri.LastIndexOfSafe('/');
            uri = position == uri.Length - 1 ? $"{uri}{parameter}" : $"{uri}/{parameter}";
            return uri;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sourcePayload"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public string BuildPayload(WebhookConfig config, string sourcePayload, IDictionary<string, object> metadata = null)
        {
            var rules = config.WebhookRequestRules.Where(l => l.Destination.Location == Location.Body).ToList();

            if (!rules.Any())
            {
                return sourcePayload;
            }

            //Any replace action replaces the payload 
            var replaceRule = rules.FirstOrDefault(r => r.Destination.RuleAction == RuleAction.Replace);
            if (replaceRule != null)
            {
                var destinationPayload = ModelParser.ParsePayloadProperty(replaceRule.Source, sourcePayload, replaceRule.Destination.Type);

                if (rules.Count <= 1)
                {
                    return destinationPayload.ToString(Formatting.None);
                }
            }

            if (metadata == null)
            {
                metadata = new Dictionary<string, object>();
            }

            JContainer payload = new JObject();
            foreach (var rule in rules)
            {
                if (rule.Destination.RuleAction != RuleAction.Add)
                {
                    continue;
                }

                object value;
                switch (rule.Source.Type)
                {
                    case DataType.Property:
                    case DataType.Model:
                        value = ModelParser.ParsePayloadProperty(rule.Source, sourcePayload, rule.Destination.Type);
                        break;

                    case DataType.HttpContent:
                        metadata.TryGetValue("HttpResponseContent", out var httpContent);
                        value = ModelParser.GetJObject(httpContent, rule.Destination.Type);
                        break;

                    case DataType.HttpStatusCode:
                        metadata.TryGetValue("HttpStatusCode", out var httpStatusCode);
                        value = ModelParser.GetJObject(httpStatusCode, rule.Destination.Type);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (string.IsNullOrWhiteSpace(rule.Destination.Path))
                {
                    payload = (JContainer) value;
                    continue;
                }

                payload.Add(new JProperty(rule.Destination.Path, value));
            }

            return payload.ToString(Formatting.None);
        }
    }
}
