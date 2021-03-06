﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using AsyncGenerator.Core.Plugins;
using AsyncGenerator.Core.Transformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	/// <summary>
	/// Adds a <see cref="ObsoleteAttribute"/> to a missing async method when its base async method as it.
	/// </summary>
	internal class ObsoleteMissingMethodTransformer : IMethodOrAccessorTransformer
	{
		public Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			return Task.CompletedTask;
		}

		public MethodTransformerResult Transform(IMethodOrAccessorTransformationResult transformResult,
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata)
		{
			var methodResult = transformResult.AnalyzationResult;
			if (!methodResult.Missing || !methodResult.Conversion.HasFlag(MethodConversion.ToAsync) || methodResult.Symbol.GetAttributes().Any(o => o.AttributeClass.Name == nameof(ObsoleteAttribute)))
			{
				return MethodTransformerResult.Skip;
			}
			var methodNode = transformResult.Transformed;
			var baseMethod = methodResult.BaseOverriddenMethod != null 
				? methodResult.RelatedMethods.FirstOrDefault(o => o.Symbol.Equals(methodResult.BaseOverriddenMethod)) 
				: methodResult.RelatedMethods.FirstOrDefault(o => methodResult.ImplementedInterfaces.Any(i => o.Symbol.Equals(i)));
			if (baseMethod?.AsyncCounterpartSymbol == null || !baseMethod.AsyncCounterpartSymbol.GetAttributes().Any(o => o.AttributeClass.Name == nameof(ObsoleteAttribute)))
			{
				return MethodTransformerResult.Skip;
			}
			namespaceMetadata.AddUsing("System");

			AttributeListSyntax obsoleteAttribute = null;
			var syntaxReference = baseMethod.AsyncCounterpartSymbol.DeclaringSyntaxReferences.SingleOrDefault();
			if (syntaxReference != null)
			{
				var baseMethodNode = syntaxReference.GetSyntax() as MethodDeclarationSyntax;
				obsoleteAttribute = baseMethodNode?.AttributeLists.FirstOrDefault(o => o.Attributes.Count == 1 && o.Attributes.First().Name.ToString() == "Obsolete");
				obsoleteAttribute = (AttributeListSyntax)new DirectiveRemover().VisitAttributeList(obsoleteAttribute);
			}

			if (obsoleteAttribute == null)
			{
				obsoleteAttribute = AttributeList(SingletonSeparatedList(Attribute(IdentifierName("Obsolete"))))
					.WithOpenBracketToken(Token(TriviaList(transformResult.LeadingWhitespaceTrivia), SyntaxKind.OpenBracketToken, TriviaList()))
					.WithCloseBracketToken(Token(TriviaList(), SyntaxKind.CloseBracketToken, TriviaList(transformResult.EndOfLineTrivia)));
			}

			methodNode = methodNode
				.WithLeadingTrivia(TriviaList(transformResult.LeadingWhitespaceTrivia))
				.WithAttributeLists(methodNode.AttributeLists.Add(obsoleteAttribute));
				
			return MethodTransformerResult.Update(methodNode);
		}
	}
}
