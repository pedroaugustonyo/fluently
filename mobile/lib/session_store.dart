import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class SessionStore {
  static const _accessTokenKey = 'access_token';
  static const _storage = FlutterSecureStorage();

  Future<String?> readAccessToken() => _storage.read(key: _accessTokenKey);

  Future<void> saveAccessToken(String accessToken) =>
      _storage.write(key: _accessTokenKey, value: accessToken);

  Future<void> clear() => _storage.delete(key: _accessTokenKey);
}
