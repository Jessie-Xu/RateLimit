# RateLimitWebApi
RateLimitWebApi solution uses ASP.NET Core framework and provides an option to throttle http requests in an application level.
The rate limit configuration can be customised in appsettings.json file.

# Projects
RateLimitModule is a class library provides a rate limit middleware for a Web application to integrate with.
RateLimitWebApi is a Web API demo project utilising RateLimitModule.
RateLimitWebAPI.IntegrationTests is a XUnit Test project
and having Microsoft.AspNetCore.Mvc.Testing Nuget package installed. 

# Testing
There are two test methods in the test project in RateLimitTests.cs
===========================
PLEASE RUN THEM SEPARATELY! 
(Because if both tests run together, there will be more API requests than the individual test expected 
and causing incorrect/failed test results)
===========================

# 1. ApiRequestsWithinRateLimitTest
The test will pass when the number of requests is less than or equal to the rate limit per configured time interval.

# 2. ApiRequestsExceedRateLimitTest
The test will first run 1 + the allowed number of requests in appsetting concurrently.

It will assert if there will be only one response returning http status 429 Too Many Request
and having a Retry-After header and a Retry-After-Seconds header

The thread will sleep the number of seconds in Retry-After-Seconds header 
and then send another API request.

It will assert if the response returning http status 200 OK. 

# Design
The rate limit strategy is implemented in the RateLimitModule as a middleware for web services to integrate with.

The configuration of the rate limit can be set in appsettings.json file.
"Interval" can be set to {n}s, {n}m, {n}h or {n}d, meaning {n} seconds, minutes, hours or days.

The default client settings in the appsettings.json is the settings of clientId "*".
The default client settings will be applied to any clients listed in the ClientList but don't have their own configurations in ClientSettings.
The default client settings will also be applied to any anonymous clients.
All anonymous clients will be treated as one requestor, meaning the default client settings will be applied in the application level.

The DateTime of a request will be stored in a queue per client. 
When the a particular requestor/client or any anonymous client makes a request,
if the number of items in the client request queue reaches the rate limit of this client,
The application will return 429 Too Many Request with the text 
"Rate limit exceeded. Try again in #{n} seconds. Allowed request rate is #{n} per {time period}".
