import 'dart:async';
import 'dart:ui';

import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import 'auth_api_client.dart';
import 'home_page.dart';
import 'session_store.dart';

const apiBaseUrl = String.fromEnvironment('API_BASE_URL');

void main() {
  runApp(const MainApp());
}

void showSonnerToast(
  BuildContext context,
  String message, {
  bool isError = false,
}) {
  final overlay = Overlay.of(context);
  late OverlayEntry entry;

  entry = OverlayEntry(
    builder: (context) => _SonnerToast(
      message: message,
      isError: isError,
      onDismissed: entry.remove,
    ),
  );

  overlay.insert(entry);
}

class _SonnerToast extends StatefulWidget {
  const _SonnerToast({
    required this.message,
    required this.isError,
    required this.onDismissed,
  });

  final bool isError;
  final String message;
  final VoidCallback onDismissed;

  @override
  State<_SonnerToast> createState() => _SonnerToastState();
}

class _SonnerToastState extends State<_SonnerToast>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;
  late final Animation<Offset> _position;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      duration: const Duration(milliseconds: 260),
      reverseDuration: const Duration(milliseconds: 180),
      vsync: this,
    );
    _position = Tween<Offset>(
      begin: const Offset(0, -0.18),
      end: Offset.zero,
    ).animate(CurvedAnimation(parent: _controller, curve: Curves.easeOutCubic));
    _controller.forward();
    _dismissAfterDelay();
  }

  Future<void> _dismissAfterDelay() async {
    await Future<void>.delayed(const Duration(seconds: 3));

    if (!mounted) {
      return;
    }

    await _controller.reverse();

    if (mounted) {
      widget.onDismissed();
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final accentColor = widget.isError
        ? const Color(0xFFF17B7B)
        : const Color(0xFF5CE0BA);

    return SafeArea(
      child: Align(
        alignment: Alignment.topCenter,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 12, 20, 0),
          child: SlideTransition(
            position: _position,
            child: FadeTransition(
              opacity: _controller,
              child: Material(
                color: Colors.transparent,
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 14,
                    vertical: 12,
                  ),
                  decoration: BoxDecoration(
                    color: const Color(0xF017202B),
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(color: const Color(0x33FFFFFF)),
                    boxShadow: const [
                      BoxShadow(
                        color: Color(0x33000000),
                        blurRadius: 24,
                        offset: Offset(0, 10),
                      ),
                    ],
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        widget.isError
                            ? Icons.error_outline_rounded
                            : Icons.check_circle_outline_rounded,
                        color: accentColor,
                        size: 19,
                      ),
                      const SizedBox(width: 10),
                      Flexible(
                        child: Text(
                          widget.message,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 13,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class MainApp extends StatefulWidget {
  const MainApp({super.key});

  @override
  State<MainApp> createState() => _MainAppState();
}

class _MainAppState extends State<MainApp> {
  final _sessionStore = SessionStore();
  AuthSession? _session;
  bool _restoringSession = true;

  @override
  void initState() {
    super.initState();
    _restoreSession();
  }

  Future<void> _restoreSession() async {
    final accessToken = await _sessionStore.readAccessToken();
    if (!mounted) {
      return;
    }

    setState(() {
      _session = accessToken == null ? null : AuthSession.restored(accessToken);
      _restoringSession = false;
    });
  }

  void _authenticate(AuthSession session) {
    unawaited(_sessionStore.saveAccessToken(session.accessToken));
    setState(() => _session = session);
  }

  void _signOut() {
    unawaited(_sessionStore.clear());
    if (mounted) {
      setState(() => _session = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF0BA88B)),
        scaffoldBackgroundColor: const Color(0xFFFDFEFE),
        textTheme: GoogleFonts.outfitTextTheme(),
      ),
      home: _restoringSession
          ? const Scaffold(body: Center(child: CircularProgressIndicator()))
          : AnimatedSwitcher(
              duration: const Duration(milliseconds: 480),
              switchInCurve: Curves.easeOutCubic,
              switchOutCurve: Curves.easeInOutCubic,
              child: _session == null
                  ? LoginPage(
                      key: const ValueKey('auth'),
                      onAuthenticated: _authenticate,
                    )
                  : HomePage(
                      key: const ValueKey('home'),
                      session: _session!,
                      onSignOut: _signOut,
                    ),
            ),
    );
  }
}

class LoginPage extends StatefulWidget {
  const LoginPage({super.key, required this.onAuthenticated});

  final ValueChanged<AuthSession> onAuthenticated;

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _serverErrors = <String, String>{};

  bool _isSubmitting = false;
  bool _isRegistering = false;
  bool _obscurePassword = true;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    setState(() {
      _serverErrors.clear();
    });

    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _isSubmitting = true;
    });

    try {
      final session = await AuthApiClient(baseUrl: apiBaseUrl).login(
        email: _emailController.text.trim(),
        password: _passwordController.text,
      );

      if (mounted) {
        widget.onAuthenticated(session);
      }
    } on AuthApiException catch (exception) {
      if (!mounted) {
        return;
      }

      setState(() {
        _serverErrors.addAll(exception.fieldErrors);
      });
      _formKey.currentState!.validate();

      if (exception.fieldErrors.isEmpty) {
        _showMessage(exception.message, isError: true);
      }
    } finally {
      if (mounted) {
        setState(() {
          _isSubmitting = false;
        });
      }
    }
  }

  void _showRegisterForm() {
    setState(() {
      _isRegistering = true;
    });
  }

  void _showLoginForm({bool showRegistrationMessage = false}) {
    setState(() {
      _isRegistering = false;
    });

    if (showRegistrationMessage) {
      _showMessage('Conta criada. Agora faça seu login.');
    }
  }

  void _showMessage(String message, {bool isError = false}) {
    showSonnerToast(context, message, isError: isError);
  }

  @override
  Widget build(BuildContext context) {
    return _AuthLayout(
      child: _isRegistering
          ? _RegisterForm(
              onLoginRequested: _showLoginForm,
              onRegistered: () => _showLoginForm(showRegistrationMessage: true),
            )
          : _buildLoginForm(),
    );
  }

  Widget _buildLoginForm() {
    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const _AuthTitle(
            title: 'Entre na sua conta',
            subtitle: 'Continue sua jornada no inglês!',
          ),
          const SizedBox(height: 18),
          _GlassInputField(
            controller: _emailController,
            hintText: 'E-mail',
            keyboardType: TextInputType.emailAddress,
            autofillHints: const [AutofillHints.email],
            prefixIcon: Icons.mail_outline_rounded,
            validator: (_) => null,
            serverError: null,
            onChanged: (_) {},
          ),
          const SizedBox(height: 10),
          _GlassInputField(
            controller: _passwordController,
            hintText: 'Senha',
            obscureText: _obscurePassword,
            autofillHints: const [AutofillHints.password],
            prefixIcon: Icons.lock_outline_rounded,
            validator: _loginPasswordValidator,
            serverError: _serverErrors['Password'],
            onChanged: (_) => _clearServerError('Password'),
            suffixIcon: IconButton(
              onPressed: () {
                setState(() {
                  _obscurePassword = !_obscurePassword;
                });
              },
              icon: Icon(
                _obscurePassword
                    ? Icons.visibility_off_outlined
                    : Icons.visibility_outlined,
              ),
            ),
          ),
          const SizedBox(height: 16),
          _PrimaryButton(
            label: 'Entrar',
            isLoading: _isSubmitting,
            onPressed: _login,
          ),
          const SizedBox(height: 10),
          _AccountPrompt(
            message: 'Ainda não tem uma conta?',
            action: 'Criar conta',
            onPressed: _showRegisterForm,
          ),
        ],
      ),
    );
  }

  void _clearServerError(String field) {
    if (_serverErrors.remove(field) != null && mounted) {
      setState(() {});
    }
  }
}

class _RegisterForm extends StatefulWidget {
  const _RegisterForm({
    required this.onLoginRequested,
    required this.onRegistered,
  });

  final VoidCallback onLoginRequested;
  final VoidCallback onRegistered;

  @override
  State<_RegisterForm> createState() => _RegisterFormState();
}

class _RegisterFormState extends State<_RegisterForm> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _passwordConfirmationController = TextEditingController();
  final _serverErrors = <String, String>{};

  bool _isSubmitting = false;
  bool _obscurePassword = true;
  bool _obscurePasswordConfirmation = true;

  bool get _isPasswordVerified =>
      _registrationPasswordValidator(_passwordController.text) == null;

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _passwordConfirmationController.dispose();
    super.dispose();
  }

  Future<void> _register() async {
    setState(() {
      _serverErrors.clear();
    });

    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _isSubmitting = true;
    });

    try {
      await AuthApiClient(baseUrl: apiBaseUrl).register(
        firstName: _firstNameController.text.trim(),
        lastName: _lastNameController.text.trim(),
        email: _emailController.text.trim(),
        password: _passwordController.text,
        passwordConfirmation: _passwordConfirmationController.text,
      );

      if (mounted) {
        widget.onRegistered();
      }
    } on AuthApiException catch (exception) {
      if (!mounted) {
        return;
      }

      setState(() {
        _serverErrors.addAll(exception.fieldErrors);
      });
      _formKey.currentState!.validate();

      if (exception.fieldErrors.isEmpty) {
        _showMessage(exception.message, isError: true);
      }
    } finally {
      if (mounted) {
        setState(() {
          _isSubmitting = false;
        });
      }
    }
  }

  void _showMessage(String message, {bool isError = false}) {
    showSonnerToast(context, message, isError: isError);
  }

  @override
  Widget build(BuildContext context) {
    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const _AuthTitle(
            title: 'Crie sua conta',
            subtitle: 'Comece sua jornada no inglês!',
          ),
          const SizedBox(height: 18),
          _GlassInputField(
            controller: _firstNameController,
            hintText: 'Primeiro nome',
            autofillHints: const [AutofillHints.givenName],
            prefixIcon: Icons.person_outline_rounded,
            validator: _firstNameValidator,
            serverError: _serverErrors['FirstName'],
            onChanged: (_) => _clearServerError('FirstName'),
          ),
          const SizedBox(height: 10),
          _GlassInputField(
            controller: _lastNameController,
            hintText: 'Sobrenome',
            autofillHints: const [AutofillHints.familyName],
            prefixIcon: Icons.person_outline_rounded,
            validator: _lastNameValidator,
            serverError: _serverErrors['LastName'],
            onChanged: (_) => _clearServerError('LastName'),
          ),
          const SizedBox(height: 10),
          _GlassInputField(
            controller: _emailController,
            hintText: 'E-mail',
            keyboardType: TextInputType.emailAddress,
            autofillHints: const [AutofillHints.email],
            prefixIcon: Icons.mail_outline_rounded,
            validator: _emailValidator,
            serverError: _serverErrors['Email'],
            onChanged: (_) => _clearServerError('Email'),
          ),
          const SizedBox(height: 10),
          _GlassInputField(
            controller: _passwordController,
            hintText: 'Senha',
            obscureText: _obscurePassword,
            autofillHints: const [AutofillHints.newPassword],
            prefixIcon: Icons.lock_outline_rounded,
            validator: _registrationPasswordValidator,
            serverError: _serverErrors['Password'],
            onChanged: (_) {
              _clearServerError('Password');
              setState(() {});
            },
            suffixIcon: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (_isPasswordVerified)
                  const Icon(
                    Icons.verified_rounded,
                    color: Color(0xFF0BA88B),
                    size: 19,
                  ),
                IconButton(
                  onPressed: () =>
                      setState(() => _obscurePassword = !_obscurePassword),
                  icon: Icon(
                    _obscurePassword
                        ? Icons.visibility_off_outlined
                        : Icons.visibility_outlined,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 10),
          _GlassInputField(
            controller: _passwordConfirmationController,
            hintText: 'Confirmar senha',
            obscureText: _obscurePasswordConfirmation,
            autofillHints: const [AutofillHints.newPassword],
            prefixIcon: Icons.lock_outline_rounded,
            validator: _passwordConfirmationValidator,
            serverError: _serverErrors['PasswordConfirmation'],
            onChanged: (_) => _clearServerError('PasswordConfirmation'),
            suffixIcon: IconButton(
              onPressed: () {
                setState(() {
                  _obscurePasswordConfirmation = !_obscurePasswordConfirmation;
                });
              },
              icon: Icon(
                _obscurePasswordConfirmation
                    ? Icons.visibility_off_outlined
                    : Icons.visibility_outlined,
              ),
            ),
          ),
          const SizedBox(height: 16),
          _PrimaryButton(
            label: 'Criar conta',
            isLoading: _isSubmitting,
            onPressed: _register,
          ),
          const SizedBox(height: 10),
          _AccountPrompt(
            message: 'Já tem uma conta?',
            action: 'Entrar',
            onPressed: widget.onLoginRequested,
          ),
        ],
      ),
    );
  }

  void _clearServerError(String field) {
    if (_serverErrors.remove(field) != null && mounted) {
      setState(() {});
    }
  }

  String? _passwordConfirmationValidator(String? value) {
    if (value == null || value.isEmpty) {
      return 'A confirmação da senha é obrigatória.';
    }

    if (value != _passwordController.text) {
      return 'A confirmação da senha deve ser igual à senha.';
    }

    return null;
  }
}

class _AuthLayout extends StatelessWidget {
  const _AuthLayout({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: DecoratedBox(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [Colors.white, Colors.white],
          ),
        ),
        child: SafeArea(
          child: LayoutBuilder(
            builder: (context, constraints) {
              return SingleChildScrollView(
                child: ConstrainedBox(
                  constraints: BoxConstraints(minHeight: constraints.maxHeight),
                  child: Column(
                    children: [
                      const _LoginHero(),
                      Padding(
                        padding: const EdgeInsets.fromLTRB(32, 22, 32, 20),
                        child: child,
                      ),
                    ],
                  ),
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}

class _LoginHero extends StatelessWidget {
  const _LoginHero();

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 246,
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Positioned(
            left: 54,
            right: 54,
            bottom: -10,
            child: Container(
              height: 78,
              decoration: const BoxDecoration(
                color: Color(0x1A62D9A2),
                borderRadius: BorderRadius.all(Radius.elliptical(180, 70)),
              ),
            ),
          ),
          Positioned(
            left: 0,
            right: 0,
            top: 18,
            child: Center(
              child: Image.asset(
                'assets/images/fluently_wordmark.png',
                width: 180,
                fit: BoxFit.contain,
              ),
            ),
          ),
          Positioned(
            left: 0,
            right: 0,
            top: 86,
            child: Center(
              child: Image.asset(
                'assets/images/fluently_mascot_auth.png',
                width: 160,
                fit: BoxFit.contain,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _AuthTitle extends StatelessWidget {
  const _AuthTitle({required this.title, required this.subtitle});

  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(
          title,
          textAlign: TextAlign.center,
          style: const TextStyle(
            color: Color(0xFF101B31),
            fontSize: 19,
            fontWeight: FontWeight.w700,
            letterSpacing: -0.8,
          ),
        ),
        const SizedBox(height: 2),
        Text(
          subtitle,
          textAlign: TextAlign.center,
          style: const TextStyle(color: Color(0xFF75839A), fontSize: 14),
        ),
      ],
    );
  }
}

class _GlassInputField extends StatelessWidget {
  const _GlassInputField({
    required this.controller,
    required this.hintText,
    required this.prefixIcon,
    required this.validator,
    required this.serverError,
    required this.onChanged,
    this.autofillHints,
    this.keyboardType,
    this.obscureText = false,
    this.suffixIcon,
  });

  final List<String>? autofillHints;
  final TextEditingController controller;
  final String hintText;
  final TextInputType? keyboardType;
  final void Function(String) onChanged;
  final bool obscureText;
  final IconData prefixIcon;
  final String? serverError;
  final Widget? suffixIcon;
  final String? Function(String?) validator;

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(18),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 14, sigmaY: 14),
        child: TextFormField(
          controller: controller,
          autofillHints: autofillHints,
          keyboardType: keyboardType,
          obscureText: obscureText,
          autovalidateMode: AutovalidateMode.onUserInteraction,
          onChanged: onChanged,
          validator: (value) => serverError ?? validator(value),
          style: const TextStyle(color: Color(0xFF101B31), fontSize: 14),
          decoration: InputDecoration(
            hintText: hintText,
            hintStyle: const TextStyle(
              color: Color(0xFF75839A),
              fontSize: 14,
              shadows: [Shadow(color: Color(0x14000000), blurRadius: 1)],
            ),
            prefixIcon: Icon(prefixIcon, color: const Color(0xFF75839A)),
            suffixIcon: suffixIcon,
            filled: true,
            fillColor: const Color(0xFFF7F9FB),
            contentPadding: const EdgeInsets.symmetric(vertical: 14),
            errorStyle: const TextStyle(fontSize: 11),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(18),
              borderSide: const BorderSide(color: Color(0x66FFFFFF)),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(18),
              borderSide: const BorderSide(color: Color(0x66FFFFFF)),
            ),
            errorBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(18),
              borderSide: const BorderSide(color: Color(0xFFD94A4A)),
            ),
            focusedErrorBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(18),
              borderSide: const BorderSide(
                color: Color(0xFFD94A4A),
                width: 1.5,
              ),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(18),
              borderSide: const BorderSide(
                color: Color(0xFF0BA88B),
                width: 1.5,
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _PrimaryButton extends StatelessWidget {
  const _PrimaryButton({
    required this.label,
    required this.isLoading,
    required this.onPressed,
  });

  final bool isLoading;
  final String label;
  final Future<void> Function() onPressed;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 44,
      child: DecoratedBox(
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(18),
          gradient: const LinearGradient(
            colors: [Color(0xFF0BA88B), Color(0xFF078F78)],
          ),
          border: Border.all(color: const Color(0x99FFFFFF)),
          boxShadow: const [
            BoxShadow(
              color: Color(0x330BA88B),
              blurRadius: 14,
              offset: Offset(0, 7),
            ),
          ],
        ),
        child: ElevatedButton(
          onPressed: isLoading ? null : onPressed,
          style: ElevatedButton.styleFrom(
            backgroundColor: Colors.transparent,
            disabledBackgroundColor: Colors.transparent,
            elevation: 0,
            foregroundColor: Colors.white,
            disabledForegroundColor: Colors.white,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(18),
            ),
          ),
          child: isLoading
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(
                    color: Colors.white,
                    strokeWidth: 2,
                  ),
                )
              : Text(
                  label,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                  ),
                ),
        ),
      ),
    );
  }
}

class _AccountPrompt extends StatelessWidget {
  const _AccountPrompt({
    required this.message,
    required this.action,
    required this.onPressed,
  });

  final String action;
  final String message;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Text(
          message,
          style: const TextStyle(color: Color(0xFF75839A), fontSize: 12),
        ),
        TextButton(
          onPressed: onPressed,
          style: TextButton.styleFrom(
            foregroundColor: const Color(0xFF0BA88B),
            padding: const EdgeInsets.only(left: 6),
          ),
          child: Text(
            action,
            style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700),
          ),
        ),
      ],
    );
  }
}

String? _firstNameValidator(String? value) {
  if (value == null || value.trim().isEmpty) {
    return 'O primeiro nome é obrigatório.';
  }

  if (value.trim().length > 100) {
    return 'O primeiro nome deve ter até 100 caracteres.';
  }

  return null;
}

String? _lastNameValidator(String? value) {
  if (value == null || value.trim().isEmpty) {
    return 'O sobrenome é obrigatório.';
  }

  if (value.trim().length > 100) {
    return 'O sobrenome deve ter até 100 caracteres.';
  }

  return null;
}

String? _emailValidator(String? value) {
  if (value == null || value.trim().isEmpty) {
    return 'O e-mail é obrigatório.';
  }

  if (value.trim().length > 320) {
    return 'O e-mail deve ter até 320 caracteres.';
  }

  if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(value.trim())) {
    return 'Informe um endereço de e-mail válido.';
  }

  return null;
}

String? _loginPasswordValidator(String? value) {
  return value == null || value.isEmpty ? 'A senha é obrigatória.' : null;
}

String? _registrationPasswordValidator(String? value) {
  final errors = <String>[];
  if (value == null || value.isEmpty) {
    errors.add('A senha é obrigatória.');
  }
  if (value == null || value.length < 8 || value.length > 128) {
    errors.add('Use entre 8 e 128 caracteres.');
  }
  if (value == null || !RegExp(r'[A-Z]').hasMatch(value)) {
    errors.add('Inclua uma letra maiúscula.');
  }
  if (value == null || !RegExp(r'\d').hasMatch(value)) {
    errors.add('Inclua um número.');
  }
  if (value == null || !RegExp(r'[^\w\s]').hasMatch(value)) {
    errors.add('Inclua um símbolo.');
  }
  return errors.isEmpty ? null : errors.join('\n');
}
