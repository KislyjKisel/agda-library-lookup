using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static AgdaLibraryLookup.Maybe<AgdaLibraryLookup.Functional.Empty>;
using static AgdaLibraryLookup.Either<AgdaLibraryLookup.Functional.Empty, AgdaLibraryLookup.Functional.Empty>;

namespace AgdaLibraryLookup.Agda.Connection
{
    using MsgOpts = CommonMessageOptions;

    public abstract class Message
    {
        public CommonMessageOptions CommonMessageOptions { get; init; }
        public HighlightingLevel    HighlightingLevel    { get; init; }
        
        public abstract IEnumerable<string> Arguments { get; }
        public /*static*/ abstract string   Command   { get; }

        public string FilePath                       => CommonMessageOptions.FilePath;
        public HighlightingMethod HighlightingMethod => CommonMessageOptions.HighlightingMethod;

        public Message(MsgOpts opts, HighlightingLevel highlightingLevel)
            => (CommonMessageOptions, HighlightingLevel) = (opts, highlightingLevel);

        public string Serialize() 
            => Arguments.Aggregate(
                new StringBuilder(
                    $"IOTCM \"{FilePath}\" {HighlightingLevel} {HighlightingMethod}( {Command} "
                ), 
                (sb, x) => sb.Append(x).Append(' ')
               )
               .Append(')').ToString();

        // object

        public override string ToString() => Serialize();
    }

    public abstract class Request : Message
    {
        public Request(MsgOpts opts, HighlightingLevel highlightingLevel)
            : base(opts, highlightingLevel) { }

        public abstract Maybe<object> ProcessResponseObject(Response response);
    }

    public abstract class Request<FilterR> : Request
    {
        public Request(MsgOpts opts, HighlightingLevel highlightingLevel)
            : base(opts, highlightingLevel) { }

        public abstract Maybe<FilterR> ProcessResponse(Response response);

        public override Maybe<object> ProcessResponseObject(Response response)
            => this.ProcessResponse(response).Map(x => (x as object)!);
    }

    public sealed class ExitMessage : Message
    {
        public ExitMessage() 
            : base(
                  new(){ FilePath = "", HighlightingMethod = HighlightingMethod.Direct }, 
                  HighlightingLevel.None
              ) { }

        // Message

        public override string Command => "Cmd_exit";
        public override IEnumerable<string> Arguments => Enumerable.Empty<string>();
    }

    public sealed class LoadRequest : Request<Either<string, string>>
    {
        public LoadRequest(string filepath)
            : base(
                new() { FilePath = filepath, HighlightingMethod = HighlightingMethod.Direct },
                HighlightingLevel.NonInteractive
            ) 
        {
            _arguments = new string[] { $"\"{filepath}\"", "[]" };
        }

        public override string Command => "Cmd_load";
        public override IEnumerable<string> Arguments => _arguments;

        public override Maybe<Either<string, string>> ProcessResponse(Response response)
        {
            if (response.Tag != "agda2-info-action" || response.Arguments.Count < 2 || String.IsNullOrWhiteSpace(response.Arguments[0]))
                return Nothing<Either<string, string>>();

            if (response.Arguments[0] == "*All Done*" || response.Arguments[0] == "*All Goals*")
                return Just(Right<string, string>(response.Arguments[1]));

            if (response.Arguments[0] == "*Error*" || response.Arguments[0] == "*All Warnings*")
                return Just(Left<string, string>(response.Arguments[1]));

            return Nothing<Either<string, string>>();
        }


        private readonly string[] _arguments;
    }

    public sealed class ComputeNormalFormGlobalRequest : Request<Either<string, string>>
    {
        public ComputeMode ComputeMode { get; init; }
        public string Expression { get; init; }

        public ComputeNormalFormGlobalRequest(MsgOpts opts, ComputeMode computeMode, string expr)
            : base(opts, HighlightingLevel.NonInteractive)
            => (ComputeMode, Expression) = (computeMode, expr);

        // Message

        public override string Command => "Cmd_compute_toplevel";
        public override IEnumerable<string> Arguments
            => new[] { ComputeMode.ToString(), $"\"{Expression}\"" };

        // Request<string>

        public override Maybe<Either<string, string>> ProcessResponse(Response response)
        {
            if(response.Tag != "agda2-info-action" || response.Arguments.Count < 2 || String.IsNullOrWhiteSpace(response.Arguments[0])) 
                return Nothing<Either<string, string>>();

            if(response.Arguments[0] == "*Normal Form*")
                return Just(Right<string, string>(response.Arguments[1]));

            if(response.Arguments[0] == "*Error*")
                return Just(Left<string, string>(response.Arguments[1]));

            return Nothing<Either<string, string>>();
        }
    }

    public sealed class InferTypeRequest : Request<Either<string, string>>
    {
        public NormalizationMode Normalization { get; init; }
        public string Expression { get; init; }

        public InferTypeRequest(MsgOpts opts, NormalizationMode normalization, string expr)
            : base(opts, HighlightingLevel.NonInteractive)
            => (Normalization, Expression) = (normalization, expr);

        // Message

        public override string Command => "Cmd_infer_toplevel";
        public override IEnumerable<string> Arguments
            => new[] { Normalization.ToString(), $"\"{Expression}\"" };

        // Request<string>

        public override Maybe<Either<string, string>> ProcessResponse(Response response)
        {
            if (response.Tag != "agda2-info-action" || response.Arguments.Count < 2 || String.IsNullOrWhiteSpace(response.Arguments[0]))
                return Nothing<Either<string, string>>();

            if (response.Arguments[0] == "*Inferred Type*")
                return Just(Right<string, string>(response.Arguments[1]));

            if (response.Arguments[0] == "*Error*")
                return Just(Left<string, string>(response.Arguments[1]));

            return Nothing<Either<string, string>>();
        }
    }

    //public sealed class ModuleContentsGlobalMessage : Message
    //{
    //    public NormalizationMode Normalization { get; init; }
    //    public string Expression { get; init; }

    //    public ModuleContentsGlobalMessage(MsgOpts opts, NormalizationMode norm, string expr)
    //        : base(opts, HighlightingLevel.None)
    //        => (Normalization, Expression) = (norm, expr);

    //    // Message

    //    public override string Command => "Cmd_show_module_contents_toplevel";
    //    public override IEnumerable<string> Arguments
    //        => new[] { Normalization.ToString(), $"\"{Expression}\"" };
    //}
}
