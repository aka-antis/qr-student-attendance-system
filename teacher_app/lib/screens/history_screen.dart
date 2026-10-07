import 'package:flutter/material.dart';
import '../api_client.dart';

class HistoryScreen extends StatefulWidget {
  final ApiClient api;
  final int meetingId;
  const HistoryScreen({super.key, required this.api, required this.meetingId});

  @override
  State<HistoryScreen> createState() => _HistoryScreenState();
}

class _HistoryScreenState extends State<HistoryScreen> {
  Map<String, dynamic>? report;
  final search = TextEditingController();

  Future<void> load() async {
    report = await widget.api.meetingReport(widget.meetingId);
    setState(() {});
  }

  @override
  void initState() {
    super.initState();
    load();
  }

  @override
  Widget build(BuildContext context) {
    final rows = ((report?['rows'] ?? []) as List)
        .where((r) =>
            search.text.isEmpty ||
            r['studentName'].toString().toLowerCase().contains(search.text.toLowerCase()))
        .toList();
    return Scaffold(
      appBar: AppBar(title: Text('Attendance #${widget.meetingId}')),
      body: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(children: [
          TextField(
              controller: search,
              decoration: const InputDecoration(labelText: 'Search student'),
              onChanged: (_) => setState(() {})),
          Text('Count: ${report?['count'] ?? '-'}',
              style: const TextStyle(fontSize: 18)),
          Expanded(
              child: ListView.builder(
                  itemCount: rows.length,
                  itemBuilder: (_, i) => ListTile(
                        title: Text(rows[i]['studentName']),
                        subtitle: Text('${rows[i]['studentCode']} @ ${rows[i]['scannedAt']}'),
                      ))),
        ]),
      ),
    );
  }
}
