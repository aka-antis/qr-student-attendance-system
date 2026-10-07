import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import '../api_client.dart';

class ScannerScreen extends StatefulWidget {
  final ApiClient api;
  final int meetingId;
  final String title;
  const ScannerScreen({super.key, required this.api, required this.meetingId, required this.title});

  @override
  State<ScannerScreen> createState() => _ScannerScreenState();
}

class _ScannerScreenState extends State<ScannerScreen> {
  String last = '';
  bool busy = false;
  final ctrl = MobileScannerController();

  Future<void> onToken(String token) async {
    if (busy || token == last) return;
    busy = true;
    last = token;
    final r = await widget.api.scan(widget.meetingId, token);
    if (!mounted) return;
    final ok = r['statusCode'] == 200;
    final dup = r['statusCode'] == 409;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(r['message'].toString()),
      backgroundColor: ok ? Colors.green : (dup ? Colors.orange : Colors.red),
      duration: const Duration(seconds: 2),
    ));
    await Future.delayed(const Duration(seconds: 2));
    busy = false;
    last = '';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Scan – ${widget.title} (#${widget.meetingId})')),
      body: Column(children: [
        Expanded(
          child: MobileScanner(
            controller: ctrl,
            onDetect: (cap) {
              final v = cap.barcodes.firstOrNull?.rawValue;
              if (v != null && v.isNotEmpty) onToken(v);
            },
          ),
        ),
        Padding(
          padding: const EdgeInsets.all(12),
          child: TextField(
            decoration: const InputDecoration(
                labelText: 'Manual token entry (if camera unavailable)',
                border: OutlineInputBorder()),
            onSubmitted: onToken,
          ),
        ),
      ]),
    );
  }
}
