using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class TextDiffWorkbench
{
    private readonly string _leftText = """
                                        public int Add(int a, int b)
                                        {
                                            return a + b;
                                        }
                                        """;

    private readonly string _rightText = """
                                         public int Add(int a, int b)
                                         {
                                             var sum = a + b;
                                             return sum;
                                         }
                                         """;

    private bool _ignoreWhitespace;
    private bool _ignoreCase;
}
