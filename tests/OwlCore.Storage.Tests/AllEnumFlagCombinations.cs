using System.Reflection;

namespace OwlCore.Storage.Tests
{
    public class AllEnumFlagCombinationsAttribute : Attribute, ITestDataSource
    {
        private readonly Type _type;

        public AllEnumFlagCombinationsAttribute(Type type)
        {
            if (type is null || !type.IsEnum)
                throw new InvalidOperationException($"Type {type} is null or not an enum");

            _type = type;
        }

        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            return GetAllValues(_type).Select(x => new[] { x });
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            CollectionAssert.AllItemsAreInstancesOfType(data, _type);

            return data.FirstOrDefault()?.ToString() ?? string.Empty;
        }

        private static IEnumerable<object> GetAllValues(Type type)
        {
            if (!type.IsEnum)
                throw new ArgumentException("Generic argument is not an enumeration type");

            var maxEnumValue = (1 << Enum.GetValues(type).Length - 1);
            return Enumerable.Range(0, maxEnumValue)
                .Select(x => Enum.Parse(type, x.ToString()));
        }
    }
}
