# RateLimitWebApi
RateLimitWebApi solution uses ASP.NET Core framework 
and provides an option to throttle http requests in an application level.
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