﻿using System.Collections.Generic;
using CaptainHook.Common.Authentication;

namespace CaptainHook.Common.Configuration
{
    /// <summary>
    /// Webhook config contains details for the webhook, eg uri and auth details
    /// </summary>
    public class WebhookConfig
    {
        public WebhookConfig()
        {
            AuthenticationConfig = new AuthenticationConfig();
            WebhookRequestRules = new List<WebhookRequestRule>();
        }

        public AuthenticationConfig AuthenticationConfig { get; set; }

        /// <summary>
        /// Destination URI for this webhook
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// The DomainType to which this webhook is registered
        /// </summary>
        public string Type { get; set; }

        //todo implement this on the calls to the webhook to select http verb
        public HttpVerb HttpVerb { get; set; }

        public List<WebhookRequestRule> WebhookRequestRules { get; set; }
        
        /// <summary>
        /// A possible callback if any is supplied. This is temporary until EDA is everywhere.
        /// </summary>
        public WebhookConfig CallbackConfig { get; set; }

        public bool CallBackEnabled => CallbackConfig != null;
    }

    public class WebhookRequestRule
    {
        public WebhookRequestRule()
        {
            Routes = new List<WebhookConfigRoute>();
            Source = new ParserLocation();
            Destination = new ParserLocation();
        }

        /// <summary>
        /// ie from payload, header, etc etc
        /// </summary>
        public ParserLocation Source { get; set; }

        /// <summary>
        /// ie uri, body, header
        /// </summary>
        public ParserLocation Destination { get; set; }
        
        /// <summary>
        /// Routes used for webhook rule types
        /// </summary>
        public List<WebhookConfigRoute> Routes { get; set; }
    }

    public class WebhookConfigRoute : WebhookConfig
    {
        /// <summary>
        /// A selector that is used in the payload to determine where the request should be routed to in the config
        /// </summary>
        public string Selector { get; set; }
    }

    public class ParserLocation
    {
        /// <summary>
        /// Path for the parameter to query from or to be placed
        /// ie: path in the message both or if it's a value in the http header
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The location of the parsed parameter or the location it should go
        /// </summary>
        public Location Location { get; set; } = Location.Body;

        /// <summary>
        /// 
        /// </summary>
        public RuleAction RuleAction { get; set; } = RuleAction.Add;

        /// <summary>
        /// The data type of the property
        /// </summary>
        public DataType Type { get; set; } = DataType.Property;
    }

    public enum HttpVerb
    {
        Get = 1,
        Put = 2,
        Post = 4,
        Patch = 5,
    }

    public enum DataType
    {
        Property = 1,
        HttpContent = 2,
        HttpStatusCode = 3,
        Model = 4,
        String = 5
    }

    public enum RuleAction
    {
        Replace = 1,
        Add = 2,
        Route = 3
    }

    public enum Location
    {
        /// <summary>
        /// Mostly used to add something to the URI of the request
        /// </summary>
        Uri = 1,

        /// <summary>
        /// The request payload body. Can come from or be attached to
        /// </summary>
        Body = 2,

        /// <summary>
        /// 
        /// </summary>
        Header = 3,

        /// <summary>
        /// Special case to get the status code of the webhook request and add it to the call back body
        /// </summary>
        HttpStatusCode = 4,

        /// <summary>
        /// Special case to get the status code of the webhook request and add it to the call back body
        /// </summary>
        HttpContent = 5,
    }
}
