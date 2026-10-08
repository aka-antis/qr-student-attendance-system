import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:teacher_app/screens/login_screen.dart';
import 'package:teacher_app/api_client.dart';

void main() {
  testWidgets('Login screen renders with base URL, username and password fields',
      (WidgetTester tester) async {
    final api = ApiClient('http://localhost:5189');
    await tester.pumpWidget(MaterialApp(home: LoginScreen(api: api)));

    expect(find.text('Teacher login'), findsOneWidget);
    expect(find.text('Login'), findsOneWidget);
    expect(find.byType(TextField), findsNWidgets(3));
  });
}
