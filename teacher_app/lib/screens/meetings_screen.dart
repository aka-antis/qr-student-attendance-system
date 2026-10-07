import 'package:flutter/material.dart';
import '../api_client.dart';
import 'scanner_screen.dart';
import 'history_screen.dart';

class MeetingsScreen extends StatefulWidget {
  final ApiClient api;
  const MeetingsScreen({super.key, required this.api});

  @override
  State<MeetingsScreen> createState() => _MeetingsScreenState();
}

class _MeetingsScreenState extends State<MeetingsScreen> {
  List<dynamic> items = [];
  String err = '';

  Future<void> load() async {
    try {
      items = await widget.api.meetings();
      err = '';
    } catch (e) {
      err = e.toString();
    }
    setState(() {});
  }

  @override
  void initState() {
    super.initState();
    load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Select meeting'), actions: [
        IconButton(onPressed: load, icon: const Icon(Icons.refresh)),
      ]),
      body: err.isNotEmpty
          ? Center(child: Text(err))
          : ListView.builder(
              itemCount: items.length,
              itemBuilder: (_, i) {
                final m = items[i];
                return ListTile(
                  title: Text('${m['courseName']} (${m['attendedCount']})'),
                  subtitle: Text('${m['date']} ${m['startTime']}-${m['endTime']}'),
                  onTap: () => Navigator.push(
                      context,
                      MaterialPageRoute(
                          builder: (_) => ScannerScreen(
                              api: widget.api,
                              meetingId: m['id'],
                              title: m['courseName']))),
                  trailing: IconButton(
                    icon: const Icon(Icons.list),
                    onPressed: () => Navigator.push(
                        context,
                        MaterialPageRoute(
                            builder: (_) => HistoryScreen(
                                api: widget.api, meetingId: m['id']))),
                  ),
                );
              }),
    );
  }
}
