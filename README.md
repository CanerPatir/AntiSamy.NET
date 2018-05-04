AntiSamy .NET [![Build status](https://ci.appveyor.com/api/projects/status/fqd8927i932hbr9j?svg=true)](https://ci.appveyor.com/project/CanerPatir/antisamy-net)
========

A .net standard library for performing configurable cleansing of HTML coming from untrusted sources.

Another way of saying that could be: It's an API that helps you make sure that clients don't supply malicious cargo code in the HTML they supply for their profile, comments, etc., 
that get persisted on the server. The term "malicious code" in regards to web applications usually mean "JavaScript." Mostly, Cascading Stylesheets are only considered malicious 
when they invoke the JavaScript. However, there are many situations where "normal" HTML and CSS can be used in a malicious manner.

How to Use
----------
First, add the dependency from Nuget
```powershall
install-package AntiSamy
```

```csharp
Policy antiSamyPolicy = Policy.FromFile("<your_antisamy_xml_file_path>")
AntiSamy antiSamy = new AntiSamy(); 
string yourDirtyInput = "<DIV><INPUT TYPE=\"IMAGE\" SRC=\"javascript:alert('XSS');\"></DIV>";
AntiSamyResult result = antiSamy.Scan(yourDirtyInput, antiSamyPolicy);

string cleanHtml = result.CleanHtml; 
IEnumerable<string> errorMessages = result.ErrorMessages;
```

Referances
----------

* [OWASP AntiSamy Project - https://www.owasp.org/index.php/Category:OWASP_AntiSamy_Project](https://www.owasp.org/index.php/Category:OWASP_AntiSamy_Project)