import 'package:flutter/material.dart';
import 'api_client.dart';
import 'screens/login_screen.dart';

void main() {
  // Web/Windows default. Android emulator: http://10.0.2.2:5189.
  // Physical device: http://<PC-LAN-IP>:5189.
  final api = ApiClient('http://localhost:5189');
  runApp(MaterialApp(
    title: 'QR Attendance',
    theme: ThemeData(useMaterial3: true, colorSchemeSeed: Colors.indigo),
    home: LoginScreen(api: api),
  ));
}
