global using System.Globalization;
global using System.Net;
global using Catalog.Application.Abstractions;
global using Catalog.Domain;
global using Dapr.Client;
global using EventPlatform.Messaging;
global using EventPlatform.Persistence;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Design;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;

// Aliased rather than imported: Ganss.Xss also declares an IHtmlSanitizer, and importing the
// namespace would make every unqualified mention of that name ambiguous against the application's
// own abstraction of the same name.
global using GanssHtmlSanitizer = Ganss.Xss.HtmlSanitizer;
