﻿using System;
using System.Linq;
using AsyncGenerator.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace AsyncGenerator.Tests.Regions
{
	[TestFixture]
	public class Fixture : BaseFixture<Input.Parent>
	{
		[Test]
		public void TestAfterAnalyzation()
		{
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.Callbacks(c => c
						.AfterAnalyzation(result =>
						{
							Assert.AreEqual(1, result.Documents.Count);
							Assert.AreEqual(1, result.Documents[0].Namespaces.Count);
							Assert.AreEqual(1, result.Documents[0].Namespaces[0].Types.Count);
							Assert.AreEqual(2, result.Documents[0].Namespaces[0].Types[0].NestedTypes.Count);
						}))
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}

		[Test]
		public void TestAfterTransformation()
		{
			var config = Configure(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol =>
					{
						return MethodConversion.Smart;
					})
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCase"), document.Transformed.ToFullString());
					})
				)
			);
			var generator = new AsyncCodeGenerator();
			Assert.DoesNotThrowAsync(async () => await generator.GenerateAsync(config));
		}
	}
}
