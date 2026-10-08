import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class ApiClient {
  String baseUrl;
  String? _token;
  static const _storage = FlutterSecureStorage();

  ApiClient(this.baseUrl);

  Future<void> loadToken() async {
    _token = await _storage.read(key: 'jwt');
  }

  Future<void> saveToken(String t) async {
    _token = t;
    await _storage.write(key: 'jwt', value: t);
  }

  Future<void> clearToken() async {
    _token = null;
    await _storage.delete(key: 'jwt');
  }

  Map<String, String> get _h => {
        'Content-Type': 'application/json',
        if (_token != null) 'Authorization': 'Bearer $_token',
      };

  Future<Map<String, dynamic>> login(String username, String password) async {
    final r = await http.post(Uri.parse('$baseUrl/api/auth/login'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'username': username, 'password': password}));
    final j = jsonDecode(r.body);
    if (r.statusCode == 200) {
      await saveToken(j['token']);
      return {'ok': true, 'fullName': j['fullName']};
    }
    return {'ok': false, 'message': j['message']?.toString() ?? r.body};
  }

  Future<List<dynamic>> meetings() async {
    final r = await http.get(Uri.parse('$baseUrl/api/meetings'), headers: _h);
    if (r.statusCode != 200) throw Exception('Failed: ${r.body}');
    return jsonDecode(r.body) as List;
  }

  /// Sends ONLY the raw QR token + meetingId. Student is resolved server-side.
  Future<Map<String, dynamic>> scan(int meetingId, String token) async {
    final r = await http.post(Uri.parse('$baseUrl/api/attendance/scan'),
        headers: _h,
        body: jsonEncode({'meetingId': meetingId, 'token': token.trim()}));
    final j = r.body.isEmpty ? {} : jsonDecode(r.body);
    return {
      'statusCode': r.statusCode,
      'status': j['status'] ?? (r.statusCode == 200 ? 'success' : 'error'),
      'message': j['message']?.toString() ?? r.body,
      'studentName': j['studentName']?.toString(),
    };
  }

  Future<Map<String, dynamic>> meetingReport(int meetingId) async {
    final r = await http.get(Uri.parse('$baseUrl/api/reports/meeting/$meetingId'), headers: _h);
    if (r.statusCode != 200) throw Exception(r.body);
    return jsonDecode(r.body);
  }

  Future<List<dynamic>> attendance({int? meetingId, String? search}) async {
    final q = <String, String>{};
    if (meetingId != null) q['meetingId'] = '$meetingId';
    if (search != null && search.isNotEmpty) q['search'] = search;
    final uri = Uri.parse('$baseUrl/api/attendance').replace(queryParameters: q.isEmpty ? null : q);
    final r = await http.get(uri, headers: _h);
    if (r.statusCode != 200) throw Exception(r.body);
    return jsonDecode(r.body) as List;
  }
}
