using System.Reflection;

namespace PropFetchSample;

class Program
{
    static void Main(string[] args)
    {
    }
}

class TestPayload
{
    public int Number { get; set; }
}

public class PropertyFetcher<TPropertyValue>
{
    public TPropertyValue GetValue(object payload, string propertyName)
    {
        if (payload.GetType().IsValueType)
        {
            throw new Exception();
        }

        PropertyFetch fetch = (PropertyFetch)typeof(PropertyFetcher<TPropertyValue>);
    }

    private static PropertyFetch propertyFetch<TPayload>(PropertyInfo propertyInfo)
    {
        return new PropertyFetchInstantiated<TPayload>(propertyInfo);
    }
}


abstract class PropertyFetch
{
    public abstract TPropertyValue GetPropertyValue(object payload); 
}

class PropertyFetchInstantiated<TPayload> : PropertyFetch
{
    Func<TPayload, TPropertyValue> getter;

    public PropertyFetchInstantiated(PropertyInfo propertyInfo)
    {
        getter = (Func<TPayload, TPropertyValue>)propertyInfo.GetMethod!.CreateDelegate(typeof(Func<TPayload, TPropertyValue>));
    }

    public override TPropertyValue GetPropertyValue(object payload)
    {
        throw new NotImplementedException()
    }
}
