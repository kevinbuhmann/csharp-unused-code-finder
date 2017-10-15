using Microsoft.CodeAnalysis;
using System;
using System.Reflection;
using Vstack.Common.Extensions;

namespace UnusedCodeFinder
{
    public static class Extensions
    {
        public static string GetIdentifer(this SyntaxNode node)
        {
            node.ValidateNotNull();

            string identifier = null;

            SyntaxNode currentNode = node;
            while (currentNode != null)
            {
                Type currentNodeType = currentNode.GetType();
                PropertyInfo nameProperty = currentNodeType.GetProperty("Name");
                PropertyInfo identiferProperty = currentNodeType.GetProperty("Identifier");

                string nodeIdentifier = (nameProperty ?? identiferProperty)?.GetValue(currentNode)?.ToString();
                string nodeIdentifierDot = nodeIdentifier == null ? string.Empty : $"{nodeIdentifier}.";

                identifier = identifier == null ? nodeIdentifier : $"{nodeIdentifierDot}{identifier}";
                currentNode = currentNode.Parent;
            }

            return identifier ?? "<unknown>";
        }
    }
}
