import 'dart:convert';

import 'package:http/http.dart' as http;

import 'auth_api_client.dart';

class FluentlyApiClient {
  FluentlyApiClient({
    required this.baseUrl,
    required this.accessToken,
    this.onUnauthorized,
  });

  final String baseUrl;
  final String accessToken;
  final void Function()? onUnauthorized;

  Future<UserProfile> getProfile() async {
    return UserProfile.fromJson(await _request('GET', '/api/v1/users/me'));
  }

  Future<UserProfile> updateProfile({
    String? firstName,
    String? lastName,
    int? proficiency,
    String? bio,
    String? profileImageBase64,
  }) async {
    return UserProfile.fromJson(
      await _request('PUT', '/api/v1/users/me', {
        'firstName': ?firstName,
        'lastName': ?lastName,
        'proficiency': ?proficiency,
        'bio': ?bio,
        'profileImageBase64': ?profileImageBase64,
      }),
    );
  }

  Future<void> updateCredentials({
    required String email,
    required String password,
    required String passwordConfirmation,
  }) async {
    await _request('PUT', '/api/v1/users/me/credentials', {
      'email': email,
      'password': password,
      'passwordConfirmation': passwordConfirmation,
    }, true);
  }

  Future<Question> getCurrentQuestion() async {
    return Question.fromJson(
      await _request('GET', '/api/v1/questions/current'),
    );
  }

  Future<Question> generateQuestion() async {
    return Question.fromJson(await _request('POST', '/api/v1/questions'));
  }

  Future<QuestionPage> getQuestions({int page = 1}) async {
    final json = await _request(
      'GET',
      '/api/v1/questions?Page=$page&PageSize=10',
    );
    return QuestionPage(
      items: ((json['items'] as List?) ?? const [])
          .cast<Map<String, dynamic>>()
          .map(Question.fromJson)
          .toList(),
      page: (json['page'] as num?)?.toInt() ?? page,
      totalPages: (json['totalPages'] as num?)?.toInt() ?? 1,
    );
  }

  Future<Question> getQuestion(String id) async {
    return Question.fromJson(await _request('GET', '/api/v1/questions/$id'));
  }

  Future<AnswerResult> submitAnswer(String id, int alternativeIndex) async {
    return AnswerResult.fromJson(
      await _request('POST', '/api/v1/questions/$id', {
        'alternativeIndex': alternativeIndex,
      }),
    );
  }

  Future<List<LeaderboardEntry>> getLeaderboard() async {
    final json = await _request(
      'GET',
      '/api/v1/leaderboard?Page=1&PageSize=20',
    );
    return ((json['items'] as List?) ?? const [])
        .cast<Map<String, dynamic>>()
        .map(LeaderboardEntry.fromJson)
        .toList();
  }

  Future<Map<String, dynamic>> _request(
    String method,
    String path, [
    Map<String, dynamic>? body,
    bool acceptsEmpty = false,
  ]) async {
    if (baseUrl.isEmpty || accessToken.isEmpty) {
      onUnauthorized?.call();
      throw const ApiException('Sua sessão expirou. Faça login novamente.');
    }

    try {
      final request = http.Request(method, Uri.parse('$baseUrl$path'));
      request.headers.addAll({
        'Authorization': 'Bearer $accessToken',
        'Content-Type': 'application/json',
      });
      if (body != null) {
        request.body = jsonEncode(body);
      }

      final response = await http.Response.fromStream(
        await request.send().timeout(const Duration(seconds: 30)),
      );
      final responseText = utf8.decode(response.bodyBytes);
      if (response.statusCode >= 200 && response.statusCode < 300) {
        if (acceptsEmpty || responseText.isEmpty) {
          return const {};
        }
        return jsonDecode(responseText) as Map<String, dynamic>;
      }

      final decoded = responseText.isEmpty
          ? const <String, dynamic>{}
          : jsonDecode(responseText) as Map<String, dynamic>;
      if (response.statusCode == 401) {
        onUnauthorized?.call();
      }
      throw ApiException(
        decoded['detail']?.toString() ??
            'Não foi possível concluir a solicitação.',
        statusCode: response.statusCode,
        fieldErrors: _errors(decoded),
      );
    } on ApiException {
      rethrow;
    } catch (_) {
      throw const ApiException(
        'Não foi possível conectar à API. Tente novamente mais tarde.',
      );
    }
  }

  Map<String, String> _errors(Map<String, dynamic> json) {
    final values = json['errors'];
    if (values is! Map<String, dynamic>) {
      return const {};
    }
    return {
      for (final entry in values.entries)
        if (entry.value is List && (entry.value as List).isNotEmpty)
          entry.key: (entry.value as List).first.toString(),
    };
  }
}

class ApiException implements Exception {
  const ApiException(
    this.message, {
    this.statusCode,
    this.fieldErrors = const {},
  });

  final String message;
  final int? statusCode;
  final Map<String, String> fieldErrors;
}

class Question {
  const Question({
    required this.id,
    required this.context,
    required this.question,
    required this.alternatives,
    required this.baseXp,
    this.questionTranslation,
    this.correctAlternative,
    this.submittedAlternativeIndex,
    this.isCorrect,
    this.awardedXp,
  });

  final String id;
  final String context;
  final String question;
  final List<QuestionAlternative> alternatives;
  final int baseXp;
  final String? questionTranslation;
  final QuestionAlternative? correctAlternative;
  final int? submittedAlternativeIndex;
  final bool? isCorrect;
  final int? awardedXp;

  factory Question.fromJson(Map<String, dynamic> json) => Question(
    id: json['id'] as String? ?? '',
    context: json['context'] as String? ?? '',
    question: json['question'] as String? ?? '',
    alternatives: ((json['alternatives'] as List?) ?? const [])
        .cast<Map<String, dynamic>>()
        .map(QuestionAlternative.fromJson)
        .toList(),
    baseXp: (json['baseXp'] as num?)?.toInt() ?? 0,
    questionTranslation: json['questionTranslation'] as String?,
    correctAlternative: json['correctAlternative'] is Map<String, dynamic>
        ? QuestionAlternative.fromJson(
            json['correctAlternative'] as Map<String, dynamic>,
          )
        : null,
    submittedAlternativeIndex: (json['submittedAlternativeIndex'] as num?)
        ?.toInt(),
    isCorrect: json['isCorrect'] as bool?,
    awardedXp: (json['awardedXp'] as num?)?.toInt(),
  );
}

class QuestionAlternative {
  const QuestionAlternative({
    required this.index,
    required this.text,
    required this.translation,
  });
  final int index;
  final String text;
  final String translation;
  factory QuestionAlternative.fromJson(Map<String, dynamic> json) =>
      QuestionAlternative(
        index: (json['index'] as num?)?.toInt() ?? 0,
        text: json['text'] as String? ?? '',
        translation: json['translation'] as String? ?? '',
      );
}

class AnswerResult {
  const AnswerResult({
    required this.isCorrect,
    required this.awardedXp,
    required this.totalXp,
    required this.currentStreak,
    required this.correctAlternative,
    required this.questionTranslation,
  });
  final bool isCorrect;
  final int awardedXp;
  final int totalXp;
  final int currentStreak;
  final QuestionAlternative correctAlternative;
  final String questionTranslation;
  factory AnswerResult.fromJson(Map<String, dynamic> json) => AnswerResult(
    isCorrect: json['isCorrect'] as bool? ?? false,
    awardedXp: (json['awardedXp'] as num?)?.toInt() ?? 0,
    totalXp: (json['totalXp'] as num?)?.toInt() ?? 0,
    currentStreak: (json['currentStreak'] as num?)?.toInt() ?? 0,
    correctAlternative: QuestionAlternative.fromJson(
      json['correctAlternative'] as Map<String, dynamic>? ?? const {},
    ),
    questionTranslation: json['questionTranslation'] as String? ?? '',
  );
}

class LeaderboardEntry {
  const LeaderboardEntry({
    required this.rank,
    required this.userId,
    required this.fullName,
    required this.totalXp,
    required this.profileImageBase64,
  });
  final int rank;
  final String userId;
  final String fullName;
  final int totalXp;
  final String? profileImageBase64;
  factory LeaderboardEntry.fromJson(Map<String, dynamic> json) =>
      LeaderboardEntry(
        rank: (json['rank'] as num?)?.toInt() ?? 0,
        userId: json['userId'] as String? ?? '',
        fullName: json['fullName'] as String? ?? '',
        totalXp: (json['totalXp'] as num?)?.toInt() ?? 0,
        profileImageBase64: json['profileImageBase64'] as String?,
      );
}

class QuestionPage {
  const QuestionPage({
    required this.items,
    required this.page,
    required this.totalPages,
  });

  final List<Question> items;
  final int page;
  final int totalPages;

  bool get hasMore => page < totalPages;
}
