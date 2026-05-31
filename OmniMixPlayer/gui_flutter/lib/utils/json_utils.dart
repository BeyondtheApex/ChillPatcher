import 'dart:collection';
import 'dart:convert' as convert;

/// A Map implementation that treats String keys case-insensitively.
class CaseInsensitiveMap<V> extends MapView<String, V> {
  CaseInsensitiveMap() : super(LinkedHashMap<String, V>(
    equals: (a, b) => a.toLowerCase() == b.toLowerCase(),
    hashCode: (key) => key.toLowerCase().hashCode,
  ));
}

/// Helper to recursively wrap maps in CaseInsensitiveMap.
dynamic _makeCaseInsensitive(dynamic val) {
  if (val is Map) {
    final map = CaseInsensitiveMap<dynamic>();
    val.forEach((k, v) {
      map[k.toString()] = _makeCaseInsensitive(v);
    });
    return map;
  } else if (val is List) {
    return val.map((item) => _makeCaseInsensitive(item)).toList();
  }
  return val;
}

/// Case-insensitive JsonCodec wrapper.
class CaseInsensitiveJson {
  const CaseInsensitiveJson();

  /// Decodes JSON source and recursively wraps all decoded maps in CaseInsensitiveMap.
  dynamic decode(String source, {Object? Function(Object? key, Object? value)? reviver}) {
    final decoded = convert.json.decode(source, reviver: reviver);
    return _makeCaseInsensitive(decoded);
  }

  /// Encodes value as JSON string.
  String encode(Object? value, {Object? Function(Object? object)? toEncodable}) {
    return convert.json.encode(value, toEncodable: toEncodable);
  }
}

/// A drop-in replacement for `dart:convert`'s `json` constant that parses
/// JSON responses case-insensitively.
const json = CaseInsensitiveJson();
