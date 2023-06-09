using System.Collections.Generic;
using System;
using JetBrains.Annotations;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace GameCollector.YamlUtils;

// Based on https://stackoverflow.com/a/40727087

[PublicAPI]
public class YamlEventStreamParserAdapter : IParser
{
    private readonly IEnumerator<ParsingEvent> enumerator;

    public YamlEventStreamParserAdapter(IEnumerable<ParsingEvent> events)
    {
        enumerator = events.GetEnumerator();
    }

    public ParsingEvent Current
    {
        get
        {
            return enumerator.Current;
        }
    }

    public bool MoveNext()
    {
        return enumerator.MoveNext();
    }
}

[PublicAPI]
public static class YamlNodeToEventStreamConverter
{
    public static IEnumerable<ParsingEvent> ConvertToEventStream(YamlStream stream)
    {
        yield return new StreamStart();
        foreach (var document in stream.Documents)
        {
            foreach (var evt in ConvertToEventStream(document))
            {
                yield return evt;
            }
        }
        yield return new StreamEnd();
    }

    public static IEnumerable<ParsingEvent> ConvertToEventStream(YamlDocument document)
    {
        yield return new DocumentStart();
        foreach (var evt in ConvertToEventStream(document.RootNode))
        {
            yield return evt;
        }
        yield return new DocumentEnd(isImplicit: false);
    }

    public static IEnumerable<ParsingEvent> ConvertToEventStream(YamlNode node)
    {
        var scalar = node as YamlScalarNode;
        if (scalar != null)
        {
            return ConvertToEventStream(scalar);
        }

        var sequence = node as YamlSequenceNode;
        if (sequence != null)
        {
            return ConvertToEventStream(sequence);
        }

        var mapping = node as YamlMappingNode;
        if (mapping != null)
        {
            return ConvertToEventStream(mapping);
        }

        throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}");
    }

    private static IEnumerable<ParsingEvent> ConvertToEventStream(YamlScalarNode scalar)
    {
        yield return new Scalar(scalar.Anchor, scalar.Tag, scalar.Value!, scalar.Style, isPlainImplicit: false, isQuotedImplicit: false);
    }

    private static IEnumerable<ParsingEvent> ConvertToEventStream(YamlSequenceNode sequence)
    {
        yield return new SequenceStart(sequence.Anchor, sequence.Tag, isImplicit: false, sequence.Style);
        foreach (var node in sequence.Children)
        {
            foreach (var evt in ConvertToEventStream(node))
            {
                yield return evt;
            }
        }
        yield return new SequenceEnd();
    }

    private static IEnumerable<ParsingEvent> ConvertToEventStream(YamlMappingNode mapping)
    {
        yield return new MappingStart(mapping.Anchor, mapping.Tag, isImplicit: false, mapping.Style);
        foreach (var pair in mapping.Children)
        {
            foreach (var evt in ConvertToEventStream(pair.Key))
            {
                yield return evt;
            }
            foreach (var evt in ConvertToEventStream(pair.Value))
            {
                yield return evt;
            }
        }
        yield return new MappingEnd();
    }
}
