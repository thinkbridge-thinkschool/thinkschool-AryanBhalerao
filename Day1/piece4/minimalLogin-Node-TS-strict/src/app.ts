import express, { Request, Response, NextFunction } from 'express';
import jwt from 'jsonwebtoken';

const app = express();
const PORT = process.env.PORT || 3000;
const JWT_SECRET = process.env.JWT_SECRET || 'YourSuperSecretKeyThatIsAtLeast32BytesLong!';

app.use(express.json());

// Interface for strictly typing the request body
interface LoginRequest {
    username?: string;
    password?: string;
}

// --- Endpoints ---

// 1. Login Endpoint
app.post('/login', (req: Request<{}, {}, LoginRequest>, res: Response): void => {
    const { username, password } = req.body;

    // Strict mode: Explicitly check for missing fields
    if (!username || !password) {
        res.status(400).json({ message: 'Username and password are required' });
        return;
    }

    // Dummy validation
    if (username === 'admin' && password === 'password123') {
        const token = jwt.sign({ username }, JWT_SECRET, { expiresIn: '2h' });
        res.json({ token });
        return;
    }

    res.status(401).json({ message: 'Unauthorized' });
});

// 2. Protected Endpoint
app.get('/secure', (req: Request, res: Response): void => {
    const authHeader = req.headers.authorization;

    // Strict mode: authHeader could be undefined
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
        res.status(401).json({ message: 'Missing or invalid authentication token' });
        return;
    }

    const token = authHeader.split(' ')[1];

    if (!token) {
        res.status(401).json({ message: 'Token not provided' });
        return;
    }

    try {
        // Verify token validity
        const decoded = jwt.verify(token, JWT_SECRET);
        res.json({ 
            message: 'Hello, authenticated user!', 
            user: decoded 
        });
    } catch (error: unknown) {
        res.status(403).json({ message: 'Invalid or expired token' });
    }
});

// Start Server
app.listen(PORT, () => {
    console.log(`Server running on http://localhost:${PORT}`);
});