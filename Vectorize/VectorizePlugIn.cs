using Rhino.PlugIns;

namespace Vectorize
{
  public class VectorizePlugIn : PlugIn
  {
    public VectorizePlugIn()
    {
      Instance = this;
    }

    public static VectorizePlugIn Instance
    {
      get; private set;
    }

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
      return LoadReturnCode.Success;
    }
  }
}