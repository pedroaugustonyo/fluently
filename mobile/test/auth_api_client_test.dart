import 'package:flutter_test/flutter_test.dart';

import 'package:fluently/auth_api_client.dart';

void main() {
  test('rejects authentication when the API URL is missing', () async {
    final client = AuthApiClient(baseUrl: '');

    await expectLater(
      client.login(email: 'ana@example.com', password: 'Password1!'),
      throwsA(
        isA<AuthApiException>().having(
          (error) => error.message,
          'message',
          'A URL da API não foi configurada.',
        ),
      ),
    );
  });

  test('parses a complete user profile', () {
    final profile = UserProfile.fromJson({
      'id': '11111111-1111-1111-1111-111111111111',
      'firstName': 'Ana',
      'lastName': 'Oliveira',
      'email': 'ana@example.com',
      'totalXp': 45,
      'currentStreak': 3,
      'proficiency': 3,
      'bio': 'Gosto de tecnologia.',
      'profileImageBase64': null,
    });

    expect(profile.fullName, 'Ana Oliveira');
    expect(profile.totalXp, 45);
    expect(profile.currentStreak, 3);
    expect(profile.needsOnboarding, isFalse);
  });

  test('requires onboarding when the profile context is incomplete', () {
    final profile = UserProfile.fromJson({
      'firstName': 'Ana',
      'lastName': 'Oliveira',
      'proficiency': 3,
      'bio': '   ',
    });

    expect(profile.needsOnboarding, isTrue);
  });
}
