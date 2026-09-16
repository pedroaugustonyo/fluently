import 'dart:convert';

import 'package:http/http.dart' as http;

class AuthApiClient {
  AuthApiClient({required this.baseUrl});

  final String baseUrl;

  Future<AuthSession> login({
    required String email,
    required String password,
  }) async {
    final response = await _post('/api/v1/auth/login', {
      'email': email,
      'password': password,
    }, expectedStatusCode: 200);

    return AuthSession.fromJson(response);
  }

  Future<void> register({
    required String firstName,
    required String lastName,
    required String email,
    required String password,
    required String passwordConfirmation,
  }) async {
    await _post('/api/v1/auth/register', {
      'firstName': firstName,
      'lastName': lastName,
      'email': email,
      'password': password,
      'passwordConfirmation': passwordConfirmation,
    }, expectedStatusCode: 201);
  }

  Future<Map<String, dynamic>> _post(
    String path,
    Map<String, String> body, {
    required int expectedStatusCode,
  }) async {
    if (baseUrl.isEmpty) {
      throw const AuthApiException(
        message: 'A URL da API não foi configurada.',
      );
    }

    try {
      final response = await http
          .post(
            Uri.parse('$baseUrl$path'),
            headers: const {'Content-Type': 'application/json'},
            body: jsonEncode(body),
          )
          .timeout(const Duration(seconds: 15));

      if (response.statusCode == expectedStatusCode) {
        return _decodeBody(utf8.decode(response.bodyBytes));
      }

      final responseBody = _decodeBody(utf8.decode(response.bodyBytes));
      final errors = <String, String>{};
      final errorValues = responseBody['errors'];

      if (errorValues is Map<String, dynamic>) {
        for (final entry in errorValues.entries) {
          final messages = entry.value;

          if (messages is List && messages.isNotEmpty) {
            errors[entry.key] = messages.first.toString();
          }
        }
      }

      if (response.statusCode == 409 && errors.isEmpty) {
        errors['Email'] =
            responseBody['detail']?.toString() ??
            'Já existe uma conta com este e-mail.';
      }

      throw AuthApiException(
        message:
            responseBody['detail']?.toString() ??
            'Não foi possível concluir a solicitação.',
        fieldErrors: errors,
      );
    } on AuthApiException {
      rethrow;
    } catch (_) {
      throw const AuthApiException(
        message: 'Não foi possível conectar à API. Tente novamente mais tarde.',
      );
    }
  }

  Map<String, dynamic> _decodeBody(String body) {
    if (body.isEmpty) {
      return const {};
    }

    return jsonDecode(body) as Map<String, dynamic>;
  }
}

class AuthSession {
  const AuthSession({required this.accessToken, required this.user});

  final String accessToken;
  final UserProfile user;

  factory AuthSession.restored(String accessToken) {
    return AuthSession(
      accessToken: accessToken,
      user: const UserProfile(
        id: '',
        firstName: '',
        lastName: '',
        email: '',
        totalXp: 0,
        currentStreak: 0,
        proficiency: null,
        bio: null,
        profileImageBase64: null,
      ),
    );
  }

  factory AuthSession.fromJson(Map<String, dynamic> json) {
    return AuthSession(
      accessToken: json['accessToken'] as String? ?? '',
      user: UserProfile.fromJson(json['user'] as Map<String, dynamic>),
    );
  }
}

class UserProfile {
  const UserProfile({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.totalXp,
    required this.currentStreak,
    required this.proficiency,
    required this.bio,
    required this.profileImageBase64,
  });

  final String id;
  final String firstName;
  final String lastName;
  final String email;
  final int totalXp;
  final int currentStreak;
  final int? proficiency;
  final String? bio;
  final String? profileImageBase64;

  String get fullName => '$firstName $lastName'.trim();
  bool get needsOnboarding =>
      proficiency == null || bio == null || bio!.trim().isEmpty;

  factory UserProfile.fromJson(Map<String, dynamic> json) {
    return UserProfile(
      id: json['id'] as String? ?? '',
      firstName: json['firstName'] as String? ?? '',
      lastName: json['lastName'] as String? ?? '',
      email: json['email'] as String? ?? '',
      totalXp: (json['totalXp'] as num?)?.toInt() ?? 0,
      currentStreak: (json['currentStreak'] as num?)?.toInt() ?? 0,
      proficiency: (json['proficiency'] as num?)?.toInt(),
      bio: json['bio'] as String?,
      profileImageBase64: json['profileImageBase64'] as String?,
    );
  }
}

class AuthApiException implements Exception {
  const AuthApiException({required this.message, this.fieldErrors = const {}});

  final String message;
  final Map<String, String> fieldErrors;
}
