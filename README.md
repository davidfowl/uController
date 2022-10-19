## Source generators for minimal APIs

The source generator will build extremely efficient code (what you would have hand written yourself) for invoking your handler methods!

## Using CI Builds

To use CI builds add the following nuget feed:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <clear />
        <add key="ucontroller" value="https://f.feedz.io/davidfowl/ucontroller/nuget/index.json" />
        <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
    </packageSources>
</configuration>
```

See the list of [versions](https://f.feedz.io/davidfowl/ucontroller/nuget/v3/packages/ucontroller/index.json)
