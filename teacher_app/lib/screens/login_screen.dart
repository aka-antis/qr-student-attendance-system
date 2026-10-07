import 'package:flutter/material.dart';
import '../api_client.dart';
import 'meetings_screen.dart';

class LoginScreen extends StatefulWidget {
  final ApiClient api;
  const LoginScreen({super.key, required this.api});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final user = TextEditingController(text: 'teacher');
  final pass = TextEditingController(text: 'teacher123');
  final base = TextEditingController(text: 'http://10.0.2.2:5189');
  String msg = '';

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Teacher login')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(children: [
          TextField(controller: base, decoration: const InputDecoration(labelText: 'API base URL')),
          TextField(controller: user, decoration: const InputDecoration(labelText: 'Username')),
          TextField(controller: pass, obscureText: true, decoration: const InputDecoration(labelText: 'Password')),
          const SizedBox(height: 12),
          ElevatedButton(
              onPressed: () async {
                // Allow overriding base URL at login time.
                widget.api.baseUrl.isNotEmpty;
                final r = await widget.api.login(user.text.trim(), pass.text);
                if (!mounted) return;
                if (r['ok'] == true) {
                  Navigator.pushReplacement(context,
                      MaterialPageRoute(builder: (_) => MeetingsScreen(api: widget.api)));
                } else {
                  setState(() => msg = r['message'].toString());
                }
              },
              child: const Text('Login')),
          Text(msg, style: const TextStyle(color: Colors.red)),
          const SizedBox(height: 8),
          const Text('Emulator: use http://10.0.2.2:PORT. Physical device: use PC LAN IP.',
              style: TextStyle(color: Colors.grey)),
        ]),
      ),
    );
  }
}
