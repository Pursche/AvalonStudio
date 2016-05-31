namespace AvalonStudio.Languages.CSharp
{
    using Avalonia.Input;
    using Avalonia.Media;
    using Projects;
    using Projects.Standard;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Xml.Linq;
    using TextEditor;
    using TextEditor.Document;
    using TextEditor.Indentation;
    using TextEditor.Rendering;
    using Utils;
    using Extensibility.Threading;
    using System.Threading.Tasks;
    using Extensibility.Languages;
    using CSharp.Rendering;
    using CSharp;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Text;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Classification;
    public class CSharpLanguageService : ILanguageService
    {
        private static ConditionalWeakTable<ISourceFile, CPlusPlusDataAssociation> dataAssociations = new ConditionalWeakTable<ISourceFile, CPlusPlusDataAssociation>();
        private JobRunner intellisenseJobRunner;

        public CSharpLanguageService()
        {
            intellisenseJobRunner = new JobRunner();

            Task.Factory.StartNew(() =>
            {
                intellisenseJobRunner.RunLoop(new CancellationToken());
            });
        }

        public string Title
        {
            get { return "C#"; }
        }

        public IProjectTemplate EmptyProjectTemplate
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Type BaseTemplateType
        {
            get
            {
                return null;
            }
        }

        private IIndentationStrategy indentationStrategy;
        public IIndentationStrategy IndentationStrategy
        {
            get
            {
                return indentationStrategy;
            }
        }

        void AddArgument(List<string> list, string argument)
        {
            if (!list.Contains(argument))
            {
                list.Add(argument);
            }
        }

        private CPlusPlusDataAssociation GetAssociatedData(ISourceFile sourceFile)
        {
            CPlusPlusDataAssociation result = null;

            if (!dataAssociations.TryGetValue(sourceFile, out result))
            {
                throw new Exception("Tried to parse file that has not been registered with the language service.");
            }

            return result;
        }
        
        public List<CodeCompletionData> CodeCompleteAt(ISourceFile file, int line, int column, List<UnsavedFile> unsavedFiles, string filter)
        {
            throw new NotImplementedException();
        }

        private static Document CreateDocumentFrom(string sourceCode)
        {
            var workspace = new AdhocWorkspace();
            Microsoft.CodeAnalysis.Solution solution = workspace.CurrentSolution;
            Project project = solution.AddProject("SyntaxHighlighter", "SyntaxHighlighter", LanguageNames.CSharp);
            Document document = project.AddDocument("source.cs", sourceCode);
            return document;
        }
  


        private Microsoft.CodeAnalysis.Document GetAndParseTranslationUnit(ISourceFile sourceFile)
        {
            var dataAssociation = GetAssociatedData(sourceFile);

            if (dataAssociation.Document == null)
            {
                dataAssociation.Document = CreateDocumentFrom(File.OpenText(sourceFile.Location).ReadToEnd());
                //dataAssociation.SyntaxTree = CSharpSyntaxTree.ParseText(SourceText.From()) as CSharpSyntaxTree;
            }
            else
            {                 
                //dataAssociation.SyntaxTree.Reparse(unsavedFiles.ToArray(), ReparseTranslationUnitFlags.None);
            }

            return dataAssociation.Document;
        }

        public CodeAnalysisResults RunCodeAnalysis(ISourceFile file, List<UnsavedFile> unsavedFiles, Func<bool> interruptRequested)
        {
            var result = new CodeAnalysisResults();

            var dataAssociation = GetAssociatedData(file);
            
            var document = GetAndParseTranslationUnit(file);
            
            SyntaxNode syntaxTreeRoot;            
            var task = document.GetSyntaxRootAsync();
            task.Wait();
            syntaxTreeRoot = task.Result;
            
            SemanticModel semanticModel;
            var semTask = document.GetSemanticModelAsync();
            semTask.Wait();
            semanticModel = semTask.Result;
            

            IEnumerable<ClassifiedSpan> classifications = Classifier.GetClassifiedSpans(semanticModel, syntaxTreeRoot.FullSpan, document.Project.Solution.Workspace);

            foreach(var classification in classifications)
            {
                switch (classification.ClassificationType)
                {
                    case ClassificationTypeNames.InterfaceName:
                    case ClassificationTypeNames.EnumName:
                    case ClassificationTypeNames.Keyword:
                        result.SyntaxHighlightingData.Add(new SyntaxHighlightingData() { Start = classification.TextSpan.Start, Length = classification.TextSpan.Length, Type = HighlightType.Keyword });
                        break;

                    case ClassificationTypeNames.ClassName:
                    case ClassificationTypeNames.StructName:
                        result.SyntaxHighlightingData.Add(new SyntaxHighlightingData() { Start = classification.TextSpan.Start, Length = classification.TextSpan.Length, Type = HighlightType.UserType });
                        break;

                    case ClassificationTypeNames.Identifier:
                        result.SyntaxHighlightingData.Add(new SyntaxHighlightingData() { Start = classification.TextSpan.Start, Length = classification.TextSpan.Length, Type = HighlightType.Identifier });
                        break;

                    case ClassificationTypeNames.Comment:
                        result.SyntaxHighlightingData.Add(new SyntaxHighlightingData() { Start = classification.TextSpan.Start, Length = classification.TextSpan.Length, Type = HighlightType.Comment });
                        break;

                    case ClassificationTypeNames.StringLiteral:
                    case ClassificationTypeNames.VerbatimStringLiteral:
                        result.SyntaxHighlightingData.Add(new SyntaxHighlightingData() { Start = classification.TextSpan.Start, Length = classification.TextSpan.Length, Type = HighlightType.Literal });
                        break;

                    case ClassificationTypeNames.Punctuation:
                        result.SyntaxHighlightingData.Add(new SyntaxHighlightingData() { Start = classification.TextSpan.Start, Length = classification.TextSpan.Length, Type = HighlightType.Punctuation });
                        break;

                    case ClassificationTypeNames.WhiteSpace:
                        //result.SyntaxHighlightingData.Add(new SyntaxHighlightingData() { Start = classification.TextSpan.Start, Length = classification.TextSpan.Length, Type = HighlightType.UserType });
                        break;

                    case ClassificationTypeNames.NumericLiteral:
                        result.SyntaxHighlightingData.Add(new SyntaxHighlightingData() { Start = classification.TextSpan.Start, Length = classification.TextSpan.Length, Type = HighlightType.Literal });
                        break;

                    case ClassificationTypeNames.PreprocessorKeyword:
                        //result.SyntaxHighlightingData.Add(new SyntaxHighlightingData() { Start = classification.TextSpan.Start, Length = classification.TextSpan.Length, Type = HighlightType.UserType });
                        break;

                    case ClassificationTypeNames.PreprocessorText:
                        //result.SyntaxHighlightingData.Add(new SyntaxHighlightingData() { Start = classification.TextSpan.Start, Length = classification.TextSpan.Length, Type = HighlightType.UserType });
                        break;

                    default:
                        Console.WriteLine(classification.ClassificationType);
                        //result.SyntaxHighlightingData.Add(new SyntaxHighlightingData() { Start = classification.TextSpan.Start, Length = classification.TextSpan.Length, Type = HighlightType.UserType });
                        break;
                }

            }

            dataAssociation.TextColorizer.SetTransformations(result.SyntaxHighlightingData);

            return result;


            dataAssociation.TextColorizer.SetTransformations(result.SyntaxHighlightingData);

            return result;
        }

        public bool CanHandle(ISourceFile file)
        {
            bool result = false;

            switch (Path.GetExtension(file.Location))
            {
                case ".cs":
                    result = true;
                    break;
            }

            if (result)
            {
                if (!(file.Project is IStandardProject))
                {
                    result = false;
                }
            }

            return result;
        }

        private void OpenBracket(TextEditor editor, AvalonStudio.TextEditor.Document.TextDocument document, string text)
        {
            if (text[0].IsOpenBracketChar() && editor.CaretIndex < document.TextLength && editor.CaretIndex > 0)
            {
                char nextChar = document.GetCharAt(editor.CaretIndex);

                if (char.IsWhiteSpace(nextChar) || nextChar.IsCloseBracketChar())
                {
                    document.Insert(editor.CaretIndex, text[0].GetCloseBracketChar().ToString());
                }
            }
        }

        private void CloseBracket(TextEditor editor, AvalonStudio.TextEditor.Document.TextDocument document, string text)
        {
            if (text[0].IsCloseBracketChar() && editor.CaretIndex < document.TextLength && editor.CaretIndex > 0)
            {
                if (document.GetCharAt(editor.CaretIndex) == text[0])
                {
                    document.Replace(editor.CaretIndex - 1, 1, string.Empty);
                }
            }
        }

        public void RegisterSourceFile(IIntellisenseControl intellisense, ICompletionAdviceControl completionAdvice, TextEditor editor, ISourceFile file, AvalonStudio.TextEditor.Document.TextDocument doc)
        {
            CPlusPlusDataAssociation association = null;

            if (dataAssociations.TryGetValue(file, out association))
            {
                throw new Exception("Source file already registered with language service.");
            }
            else
            {
                association = new CPlusPlusDataAssociation(doc);
                dataAssociations.Add(file, association);
            };
        }

        public IList<IDocumentLineTransformer> GetDocumentLineTransformers(ISourceFile file)
        {
            var associatedData = GetAssociatedData(file);

            return associatedData.DocumentLineTransformers;
        }

        public IList<IBackgroundRenderer> GetBackgroundRenderers(ISourceFile file)
        {
            var associatedData = GetAssociatedData(file);

            return associatedData.BackgroundRenderers;
        }

        public void UnregisterSourceFile(TextEditor editor, ISourceFile file)
        {
            var association = GetAssociatedData(file);
            
            dataAssociations.Remove(file);
        }

        public int Format(AvalonStudio.TextEditor.Document.TextDocument textDocument, uint offset, uint length, int cursor)
        {
            throw new NotImplementedException();
        }
        

        public Symbol GetSymbol(ISourceFile file, List<UnsavedFile> unsavedFiles, int offset)
        {
            var associatedData = GetAssociatedData(file);

            return null;
        }
        

        public int Comment(AvalonStudio.TextEditor.Document.TextDocument textDocument, ISegment segment, int caret = -1, bool format = true)
        {
            int result = caret;

            IEnumerable<IDocumentLine> lines = VisualLineGeometryBuilder.GetLinesForSegmentInDocument(textDocument, segment);

            textDocument.BeginUpdate();

            foreach (var line in lines)
            {
                textDocument.Insert(line.Offset, "//");
            }

            if (format)
            {
                result = Format(textDocument, (uint)segment.Offset, (uint)segment.Length, caret);
            }

            textDocument.EndUpdate();

            return result;
        }


        public int UnComment(AvalonStudio.TextEditor.Document.TextDocument textDocument, ISegment segment, int caret = -1, bool format = true)
        {
            int result = caret;

            IEnumerable<IDocumentLine> lines = VisualLineGeometryBuilder.GetLinesForSegmentInDocument(textDocument, segment);

            textDocument.BeginUpdate();

            foreach (var line in lines)
            {
                var index = textDocument.GetText(line).IndexOf("//");

                if (index >= 0)
                {
                    textDocument.Replace(line.Offset + index, 2, string.Empty);
                }
            }

            if (format)
            {
                result = Format(textDocument, (uint)segment.Offset, (uint)segment.Length, caret);
            }

            textDocument.EndUpdate();

            return result;
        }

        public List<Symbol> GetSymbols(ISourceFile file, List<UnsavedFile> unsavedFiles, string name)
        {
            throw new NotImplementedException();
        }
    }

    class CPlusPlusDataAssociation
    {
        public CPlusPlusDataAssociation(AvalonStudio.TextEditor.Document.TextDocument textDocument)
        {
            BackgroundRenderers = new List<IBackgroundRenderer>();
            DocumentLineTransformers = new List<IDocumentLineTransformer>();

            TextColorizer = new TextColoringTransformer(textDocument);
            TextMarkerService = new TextMarkerService(textDocument);

            BackgroundRenderers.Add(new BracketMatchingBackgroundRenderer());
            BackgroundRenderers.Add(TextMarkerService);

            DocumentLineTransformers.Add(TextColorizer);            
        }
        
        public TextColoringTransformer TextColorizer { get; private set; }
        public TextMarkerService TextMarkerService { get; private set; }
        public List<IBackgroundRenderer> BackgroundRenderers { get; private set; }
        public List<IDocumentLineTransformer> DocumentLineTransformers { get; private set; }
        public EventHandler<KeyEventArgs> TunneledKeyUpHandler { get; set; }
        public EventHandler<KeyEventArgs> TunneledKeyDownHandler { get; set; }
        public EventHandler<KeyEventArgs> KeyUpHandler { get; set; }
        public EventHandler<KeyEventArgs> KeyDownHandler { get; set; }
        public EventHandler<TextInputEventArgs> TextInputHandler { get; set; }        
        public SyntaxTree SyntaxTree { get; set; }
        public Microsoft.CodeAnalysis.Document Document { get; set; }
    }
}
