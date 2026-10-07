import 'package:flutter/material.dart';
import 'api_client.dart';
import 'screens/login_screen.dart';

void main() {
  // Change to your PC LAN IP when testing on a physical device.
  final api = ApiClient('http://10.0.2.2:5189');
  runApp(MaterialApp(
    title: 'QR Attendance',
    theme: ThemeData(useMaterial3: true, colorSchemeSeed: Colors.indigo),
    home: LoginScreen(api: api),
  ));
}
