#r "nuget: AgileObjects.AgileMapper, 0.23.0"
using AgileObjects.AgileMapper;
public class TestClass
{
    public TestClass()
    {
        IMapper mapper = Mapper.CreateNew();
    }
}